using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Lifecycle;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves admission, completion, and replacement ownership for ordinary UI work.</summary>
[TestClass]
public sealed class AsyncWorkOwnerTests
{
    /// <summary>Proves a completed run remains unavailable while its completion callback is active.</summary>
    [TestMethod]
    public async Task TryStartWhenCompletedRunIsStillCompletingRejectsBothRoutes()
    {
        TaskCompletionSource replacement = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource checkedAdmission = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<bool> ordinary = static () => true;
        Func<bool> intent = static () => true;
        bool ordinaryAccepted = true;
        bool intentAccepted = true;
        AsyncWorkOwner owner = new(
            _ => Assert.Fail("No defect expected."),
            static () => new CancellationTokenSource(),
            () =>
            {
                if (!checkedAdmission.Task.IsCompleted)
                {
                    ordinaryAccepted = ordinary();
                    intentAccepted = intent();
                    checkedAdmission.SetResult();
                }
            });
        ordinary = () => owner.TryStart(_ => replacement.Task);
        intent = () => owner.TryStartIntent(UserIntent.Escape, _ => Task.CompletedTask);

        Assert.IsTrue(owner.TryStart(_ => Task.CompletedTask));
        await checkedAdmission.Task;
        replacement.SetResult();
        await owner.StopAsync();

        Assert.IsFalse(ordinaryAccepted);
        Assert.IsFalse(intentAccepted);
    }

    /// <summary>Proves a queued duplicate completion cannot detach a successful replacement run.</summary>
    [TestMethod]
    public async Task TryStartWhenStaleCompletionRunsKeepsReplacementOwned()
    {
        ControlledSynchronizationContext context = new();
        AsyncWorkOwner owner = new(_ => Assert.Fail("No defect expected."));
        SynchronizationContext? previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            Assert.IsTrue(owner.TryStart(_ => Task.CompletedTask));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        await context.Posted;
        TaskCompletionSource replacement = new(TaskCreationOptions.RunContinuationsAsynchronously);

        bool started = owner.TryStart(_ => replacement.Task);
        context.ExecuteOne();
        bool stillOwned = owner.HasOwnedWork;
        Task stopping = owner.StopAsync();
        bool pending = !stopping.IsCompleted;
        replacement.SetResult();
        await stopping;

        Assert.IsTrue(started);
        Assert.IsTrue(stillOwned);
        Assert.IsTrue(pending);
        Assert.IsFalse(owner.HasOwnedWork);
    }

    /// <summary>Proves successful completed work may be replaced without overlapping runs.</summary>
    [TestMethod]
    public async Task TryStartWhenPriorWorkCompletedDisposesItAndStartsReplacement()
    {
        int starts = 0;
        ControlledSynchronizationContext context = new();
        AsyncWorkOwner owner = new(_ => Assert.Fail("No defect expected."));
        SynchronizationContext? previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            Assert.IsTrue(owner.TryStart(_ =>
            {
                starts++;
                return Task.CompletedTask;
            }));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        await context.Posted;

        Assert.IsTrue(owner.TryStart(_ =>
        {
            starts++;
            return Task.CompletedTask;
        }));
        context.ExecuteOne();
        await owner.StopAsync();

        Assert.AreEqual(2, starts);
    }

    /// <summary>Proves running work rejects overlap and a stopped owner is idempotent.</summary>
    [TestMethod]
    public async Task TryStartWhenWorkIsRunningRejectsOverlapUntilStopCompletes()
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AsyncWorkOwner owner = new(_ => Assert.Fail("No defect expected."));

        Assert.IsTrue(owner.TryStart(_ => completion.Task));
        Assert.IsFalse(owner.TryStart(_ => Task.CompletedTask));
        completion.SetResult();
        await owner.StopAsync();
        await owner.StopAsync();

        Assert.IsNull(owner.Fault);
    }

    /// <summary>Proves a synchronous work-factory defect stays with its synchronous caller.</summary>
    [TestMethod]
    public void TryStartWhenFactoryThrowsDisposesCancellationAndRethrows()
    {
        InvalidOperationException defect = new("Synchronous launch defect.");
        List<Exception> observed = [];
        int disposals = 0;
        using CancellationTokenSource cancellation = new();
        AsyncWorkOwner owner = new(
            observed.Add,
            () => cancellation,
            () => disposals++);

        InvalidOperationException thrown = Assert.ThrowsExactly<InvalidOperationException>(
            () => owner.TryStart(_ => throw defect));

        Assert.AreSame(defect, thrown);
        Assert.IsNull(owner.Fault);
        Assert.IsEmpty(observed);
        Assert.AreEqual(1, disposals);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cancellation.Token);
    }

    /// <summary>Proves required owner dependencies reject absence at their boundary.</summary>
    [TestMethod]
    public void RequiredArgumentsWhenAbsentThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new AsyncWorkOwner(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new AsyncWorkOwner(_ => { }, null!, () => { }));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new AsyncWorkOwner(_ => { }, static () => null!, null!));
        AsyncWorkOwner owner = new(_ => { });
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => owner.TryStart(null!));
        AsyncWorkOwner nullCancellation = new(
            _ => { },
            static () => null!,
            () => { });
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            nullCancellation.TryStart(_ => Task.CompletedTask));
    }

    /// <summary>Proves a null work task is rejected and its allocated cancellation source is disposed.</summary>
    [TestMethod]
    public void TryStartWhenFactoryReturnsNullRejectsAndDisposesCancellation()
    {
        using CancellationTokenSource cancellation = new();
        AsyncWorkOwner owner = new(
            _ => Assert.Fail("No asynchronous defect expected."),
            () => cancellation,
            () => { });

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => owner.TryStart(_ => null!));

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cancellation.Token);
        Assert.IsFalse(owner.HasOwnedWork);
    }

    /// <summary>Proves natural completion releases and disposes the exact cancellation source.</summary>
    [TestMethod]
    public async Task TryStartWhenWorkCompletesReleasesAndDisposesCancellation()
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AsyncWorkOwner owner = new(
            _ => Assert.Fail("No defect expected."),
            () => cancellation,
            disposed.SetResult);

        Assert.IsTrue(owner.TryStart(_ => Task.CompletedTask));
        await disposed.Task;
        await owner.StopAsync();

        Assert.IsFalse(owner.HasOwnedWork);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cancellation.Token);
    }
}
