using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Lifecycle;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves fault publication, cancellation, and shutdown ordering for owned UI work.</summary>
[TestClass]
public sealed class AsyncWorkOwnerFaultAndShutdownTests
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
        await owner.StopAsync();
        Assert.IsFalse(owner.HasOwnedWork);
    }

    /// <summary>Proves Stop waits until a fault observer finishes publishing the defect.</summary>
    [TestMethod]
    public async Task StopAsyncWhenDefectObserverPendingWaitsForPublication()
    {
        InvalidOperationException defect = new("Injected work defect.");
        TaskCompletionSource work = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<Task> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<Task> stop = static () => Task.CompletedTask;
        Func<bool> hasOwnedWork = static () => false;
        Exception? observed = null;
        bool pendingDuringObserver = false;
        bool ownedDuringObserver = false;
        AsyncWorkOwner owner = new(exception =>
        {
            observed = exception;
            Task stopping = stop();
            pendingDuringObserver = !stopping.IsCompleted;
            ownedDuringObserver = hasOwnedWork();
            entered.SetResult(stopping);
        });
        stop = owner.StopAsync;
        hasOwnedWork = () => owner.HasOwnedWork;
        Assert.IsTrue(owner.TryStart(_ => work.Task));

        work.SetException(defect);
        Task stopping = await entered.Task;
        await stopping;

        Assert.AreSame(defect, observed);
        Assert.IsTrue(pendingDuringObserver);
        Assert.IsTrue(ownedDuringObserver);
        Assert.IsFalse(owner.HasOwnedWork);
    }

    /// <summary>Proves a throwing observer still releases the owned run and its waiter once.</summary>
    [TestMethod]
    public async Task StopAsyncWhenDefectObserverThrowsStillCompletesOwnedRun()
    {
        InvalidOperationException workDefect = new("Injected work defect.");
        InvalidOperationException observerDefect = new("Injected observer defect.");
        ControlledSynchronizationContext context = new();
        int disposals = 0;
        AsyncWorkOwner owner = new(
            _ => throw observerDefect,
            static () => new CancellationTokenSource(),
            () => disposals++);
        SynchronizationContext? previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            Assert.IsTrue(owner.TryStart(_ => Task.FromException(workDefect)));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        await context.Posted;
        Task stopping = owner.StopAsync();

        InvalidOperationException thrown = Assert.ThrowsExactly<InvalidOperationException>(context.ExecuteOne);
        await stopping;

        Assert.AreSame(observerDefect, thrown);
        Assert.AreSame(workDefect, owner.Fault);
        Assert.AreEqual(1, disposals);
        Assert.IsFalse(owner.HasOwnedWork);
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

    /// <summary>Proves stopping an idle owner permanently rejects both admission routes.</summary>
    [TestMethod]
    public async Task StopAsyncWhenIdleRejectsOrdinaryAndIntentStarts()
    {
        int starts = 0;
        AsyncWorkOwner owner = new(_ => Assert.Fail("No defect expected."));

        await owner.StopAsync();
        await owner.StopAsync();
        bool ordinary = owner.TryStart(_ =>
        {
            starts++;
            return Task.CompletedTask;
        });
        bool intent = owner.TryStartIntent(UserIntent.Escape, _ =>
        {
            starts++;
            return Task.CompletedTask;
        });

        Assert.IsFalse(ordinary);
        Assert.IsFalse(intent);
        Assert.AreEqual(0, starts);
    }

    /// <summary>Proves Stop rejects starts while admitted work drains and after it completes.</summary>
    [TestMethod]
    public async Task StopAsyncWhenWorkPendingRejectsStartsDuringAndAfterDrain()
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int starts = 0;
        AsyncWorkOwner owner = new(_ => Assert.Fail("No defect expected."));
        Assert.IsTrue(owner.TryStart(_ => completion.Task));

        Task stopping = owner.StopAsync();
        bool interruptDuringDrain = owner.TryStartIntent(UserIntent.Escape, _ =>
        {
            starts++;
            return Task.CompletedTask;
        });
        Assert.IsFalse(stopping.IsCompleted);
        completion.SetResult();
        await stopping;
        bool ordinaryAfterDrain = owner.TryStart(_ =>
        {
            starts++;
            return Task.CompletedTask;
        });
        bool intentAfterDrain = owner.TryStartIntent(UserIntent.MoveNext, _ =>
        {
            starts++;
            return Task.CompletedTask;
        });

        Assert.IsFalse(interruptDuringDrain);
        Assert.IsFalse(ordinaryAfterDrain);
        Assert.IsFalse(intentAfterDrain);
        Assert.AreEqual(0, starts);
    }

    /// <summary>Proves completed work cannot be replaced once Stop latches.</summary>
    [TestMethod]
    public async Task StopAsyncWhenWorkCompletedRejectsReplacement()
    {
        int starts = 0;
        AsyncWorkOwner owner = new(_ => Assert.Fail("No defect expected."));
        Assert.IsTrue(owner.TryStart(_ => Task.CompletedTask));

        await owner.StopAsync();
        bool replacement = owner.TryStart(_ =>
        {
            starts++;
            return Task.CompletedTask;
        });

        Assert.IsFalse(replacement);
        Assert.AreEqual(0, starts);
    }

    /// <summary>Proves a cancellation callback defect still cancels and drains both admitted runs.</summary>
    [TestMethod]
    public async Task StopAsyncWhenCancellationCallbackThrowsStillDrainsWorkAndInterrupt()
    {
        InvalidOperationException defect = new("Injected cancellation defect.");
        TaskCompletionSource workDone = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource interruptDone = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken workToken = default;
        CancellationToken interruptToken = default;
        int disposals = 0;
        AsyncWorkOwner owner = new(
            _ => Assert.Fail("Work itself must not fault."),
            static () => new CancellationTokenSource(),
            () => disposals++);
        Assert.IsTrue(owner.TryStart(token =>
        {
            workToken = token;
            _ = token.Register(() => throw defect);
            return workDone.Task;
        }));
        Assert.IsTrue(owner.TryStartIntent(UserIntent.Escape, token =>
        {
            interruptToken = token;
            return interruptDone.Task;
        }));

        Task stopping = owner.StopAsync();
        bool pendingBeforeCleanup = !stopping.IsCompleted;
        bool workCancelled = workToken.IsCancellationRequested;
        bool interruptCancelled = interruptToken.IsCancellationRequested;
        workDone.SetResult();
        interruptDone.SetResult();
        AggregateException thrown = await Assert.ThrowsExactlyAsync<AggregateException>(() => stopping);

        Assert.IsTrue(pendingBeforeCleanup);
        Assert.IsTrue(workCancelled);
        Assert.IsTrue(interruptCancelled);
        Assert.AreSame(defect, thrown.InnerException);
        Assert.AreEqual(2, disposals);
        Assert.IsFalse(owner.HasOwnedWork);
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
