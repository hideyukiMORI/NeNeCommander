using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Lifecycle;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves deterministic ownership, fault observation, and shutdown ordering for UI work.</summary>
[TestClass]
public sealed class AsyncWorkOwnerTests
{
    /// <summary>Proves a fault is observed exactly once and blocks replacement work.</summary>
    [TestMethod]
    public async Task TryStartWhenWorkFaultsObservesExactDefectAndRejectsReplacement()
    {
        InvalidOperationException defect = new("Injected defect.");
        List<Exception> observed = [];
        TaskCompletionSource<Exception> observation = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AsyncWorkOwner owner = new(exception =>
        {
            observed.Add(exception);
            observation.SetResult(exception);
        });

        bool started = owner.TryStart(_ => Task.FromException(defect));
        bool replacement = owner.TryStart(_ => Task.CompletedTask);
        _ = await observation.Task;

        Assert.IsTrue(started);
        Assert.IsFalse(replacement);
        Assert.AreSame(defect, owner.Fault);
        Assert.HasCount(1, observed);
        Assert.AreSame(defect, observed[0]);
        Assert.IsFalse(owner.HasOwnedWork);
        await owner.StopAsync();
    }

    /// <summary>Proves shutdown cancels, awaits the work cleanup, and disposes the token source.</summary>
    [TestMethod]
    public async Task StopAsyncCancelsAwaitsThenDisposesOwnedToken()
    {
        List<string> events = [];
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken ownedToken = default;
        AsyncWorkOwner owner = new(
            _ => Assert.Fail("Cancellation is not a defect."),
            static () => new CancellationTokenSource(),
            () => events.Add("disposed"));
        _ = owner.TryStart(async cancellationToken =>
        {
            ownedToken = cancellationToken;
            started.SetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            finally
            {
                events.Add("awaited");
            }
        });
        await started.Task;

        await owner.StopAsync();

        Assert.IsTrue(ownedToken.IsCancellationRequested);
        Assert.HasCount(2, events);
        Assert.AreEqual("awaited", events[0]);
        Assert.AreEqual("disposed", events[1]);
    }

    /// <summary>Proves successful completed work may be replaced without overlapping runs.</summary>
    [TestMethod]
    public async Task TryStartWhenPriorWorkCompletedDisposesItAndStartsReplacement()
    {
        int starts = 0;
        AsyncWorkOwner owner = new(_ => Assert.Fail("No defect expected."));

        Assert.IsTrue(owner.TryStart(_ =>
        {
            starts++;
            return Task.CompletedTask;
        }));
        Assert.IsTrue(owner.TryStart(_ =>
        {
            starts++;
            return Task.CompletedTask;
        }));
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

        Assert.IsFalse(owner.HasOwnedWork);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cancellation.Token);
    }

    /// <summary>Proves concurrent defects are preserved as one aggregate rather than losing either defect.</summary>
    [TestMethod]
    public async Task TryStartWhenWorkHasMultipleDefectsObservesAggregate()
    {
        InvalidOperationException first = new("First defect.");
        NotSupportedException second = new("Second defect.");
        TaskCompletionSource<Exception> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AsyncWorkOwner owner = new(observed.SetResult);

        Assert.IsTrue(owner.TryStart(_ => Task.WhenAll(
            Task.FromException(first),
            Task.FromException(second))));
        AggregateException aggregate = Assert.IsInstanceOfType<AggregateException>(await observed.Task);

        Assert.HasCount(2, aggregate.InnerExceptions);
        Assert.AreSame(first, aggregate.InnerExceptions[0]);
        Assert.AreSame(second, aggregate.InnerExceptions[1]);
        Assert.AreSame(aggregate, owner.Fault);
    }
}
