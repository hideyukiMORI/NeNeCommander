using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
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

    /// <summary>Proves an intent with no running work starts as ordinary work.</summary>
    [TestMethod]
    public async Task TryStartIntentWhenNothingRunsStartsOrdinaryWork()
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AsyncWorkOwner owner = new(_ => Assert.Fail("No defect expected."));

        bool started = owner.TryStartIntent(UserIntent.MoveNext, _ => completion.Task);
        bool overlap = owner.TryStart(_ => Task.CompletedTask);
        completion.SetResult();
        await owner.StopAsync();

        Assert.IsTrue(started);
        Assert.IsFalse(overlap);
    }

    /// <summary>
    /// Proves running work admits exactly one Escape interrupt: another intent, a second Escape, and
    /// ordinary work are rejected while it runs, and a new interrupt starts once the first finished.
    /// </summary>
    [TestMethod]
    public async Task TryStartIntentWhenWorkRunsAdmitsOneEscapeInterrupt()
    {
        TaskCompletionSource running = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource interrupt = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ControlledSynchronizationContext context = new();
        int interrupts = 0;
        AsyncWorkOwner owner = new(_ => Assert.Fail("No defect expected."));
        Assert.IsTrue(owner.TryStart(_ => running.Task));
        Assert.IsTrue(owner.HasOwnedWork);

        bool otherIntent = owner.TryStartIntent(UserIntent.MoveNext, _ => Task.CompletedTask);
        SynchronizationContext? previous = SynchronizationContext.Current;
        bool first;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            first = owner.TryStartIntent(UserIntent.Escape, _ =>
            {
                interrupts++;
                return interrupt.Task;
            });
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        bool second = owner.TryStartIntent(UserIntent.Escape, _ =>
        {
            interrupts++;
            return Task.CompletedTask;
        });
        bool ordinary = owner.TryStart(_ => Task.CompletedTask);
        interrupt.SetResult();
        await context.Posted;
        bool afterInterrupt = owner.TryStartIntent(UserIntent.Escape, _ =>
        {
            interrupts++;
            return Task.CompletedTask;
        });
        context.ExecuteOne();
        running.SetResult();
        await owner.StopAsync();

        Assert.IsFalse(otherIntent);
        Assert.IsTrue(first);
        Assert.IsFalse(second);
        Assert.IsFalse(ordinary);
        Assert.IsTrue(afterInterrupt);
        Assert.AreEqual(2, interrupts);
        Assert.IsFalse(owner.HasOwnedWork);
    }

    /// <summary>Proves shutdown cancels and awaits both the running work and its interrupt.</summary>
    [TestMethod]
    public async Task StopAsyncWhenInterruptRunsCancelsAndAwaitsBoth()
    {
        CancellationToken workToken = default;
        CancellationToken interruptToken = default;
        bool interruptFinished = false;
        AsyncWorkOwner owner = new(_ => Assert.Fail("Cancellation is not a defect."));
        _ = owner.TryStart(async cancellationToken =>
        {
            workToken = cancellationToken;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        });
        _ = owner.TryStartIntent(UserIntent.Escape, async cancellationToken =>
        {
            interruptToken = cancellationToken;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            for (int step = 0; step < 8; step++)
            {
                await Task.Yield();
            }
            interruptFinished = true;
        });

        await owner.StopAsync();

        Assert.IsTrue(workToken.IsCancellationRequested);
        Assert.IsTrue(interruptToken.IsCancellationRequested);
        Assert.IsTrue(interruptFinished);
        Assert.IsFalse(owner.HasOwnedWork);
    }

    /// <summary>Proves a faulted interrupt is observed and blocks every later start, including another interrupt.</summary>
    [TestMethod]
    public async Task TryStartIntentWhenInterruptFaultsObservesDefectAndRejectsLaterWork()
    {
        InvalidOperationException defect = new("Interrupt defect.");
        TaskCompletionSource running = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<Exception> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AsyncWorkOwner owner = new(observed.SetResult);
        _ = owner.TryStart(_ => running.Task);

        bool interrupted = owner.TryStartIntent(UserIntent.Escape, _ => Task.FromException(defect));
        Exception fault = await observed.Task;
        bool again = owner.TryStartIntent(UserIntent.Escape, _ => Task.CompletedTask);
        running.SetResult();
        await owner.StopAsync();
        bool replacement = owner.TryStartIntent(UserIntent.MoveNext, _ => Task.CompletedTask);

        Assert.IsTrue(interrupted);
        Assert.AreSame(defect, fault);
        Assert.AreSame(defect, owner.Fault);
        Assert.IsFalse(again);
        Assert.IsFalse(replacement);
    }

    /// <summary>Proves a completed interrupt whose release is still pending is released before its replacement starts.</summary>
    [TestMethod]
    public async Task TryStartIntentWhenPriorInterruptCompletedReleasesItFirst()
    {
        TaskCompletionSource running = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ControlledSynchronizationContext context = new();
        int disposals = 0;
        AsyncWorkOwner owner = new(
            _ => Assert.Fail("No defect expected."),
            static () => new CancellationTokenSource(),
            () => disposals++);
        _ = owner.TryStart(_ => running.Task);
        SynchronizationContext? previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            _ = owner.TryStartIntent(UserIntent.Escape, _ => Task.CompletedTask);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        await context.Posted;

        bool replacement = owner.TryStartIntent(UserIntent.Escape, _ => Task.CompletedTask);
        context.ExecuteOne();
        running.SetResult();
        await owner.StopAsync();

        Assert.IsTrue(replacement);
        Assert.AreEqual(3, disposals);
    }

    /// <summary>Proves ordinary work stays rejected while an interrupt outlives the work it interrupted.</summary>
    [TestMethod]
    public async Task TryStartWhenInterruptOutlivesWorkRejectsUntilItCompletes()
    {
        TaskCompletionSource running = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource interrupt = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ControlledSynchronizationContext runningContext = new();
        ControlledSynchronizationContext interruptContext = new();
        AsyncWorkOwner owner = new(_ => Assert.Fail("No defect expected."));
        SynchronizationContext? previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(runningContext);
            _ = owner.TryStart(_ => running.Task);
            SynchronizationContext.SetSynchronizationContext(interruptContext);
            _ = owner.TryStartIntent(UserIntent.Escape, _ => interrupt.Task);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        running.SetResult();
        await runningContext.Posted;

        bool whileInterrupting = owner.TryStart(_ => Task.CompletedTask);
        interrupt.SetResult();
        await interruptContext.Posted;
        bool afterInterrupt = owner.TryStart(_ => Task.CompletedTask);
        runningContext.ExecuteOne();
        interruptContext.ExecuteOne();
        await owner.StopAsync();

        Assert.IsFalse(whileInterrupting);
        Assert.IsTrue(afterInterrupt);
    }

    /// <summary>Proves a completed interrupt's defect is observed before a replacement interrupt could start.</summary>
    [TestMethod]
    public async Task TryStartIntentWhenPriorInterruptFaultedRejectsReplacementAtOnce()
    {
        InvalidOperationException defect = new("Interrupt defect.");
        TaskCompletionSource running = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AsyncWorkOwner owner = new(_ => { });
        _ = owner.TryStart(_ => running.Task);
        _ = owner.TryStartIntent(UserIntent.Escape, _ => Task.FromException(defect));

        bool replacement = owner.TryStartIntent(UserIntent.Escape, _ => Task.CompletedTask);
        running.SetResult();
        await owner.StopAsync();

        Assert.IsFalse(replacement);
        Assert.AreSame(defect, owner.Fault);
    }

    /// <summary>Proves the intent route rejects absent arguments, also while work runs.</summary>
    [TestMethod]
    public async Task TryStartIntentWhenArgumentsAreAbsentThrowsArgumentNullException()
    {
        TaskCompletionSource running = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AsyncWorkOwner owner = new(_ => { });

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => owner.TryStartIntent(null!, _ => Task.CompletedTask));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => owner.TryStartIntent(UserIntent.Escape, null!));
        _ = owner.TryStart(_ => running.Task);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => owner.TryStartIntent(UserIntent.Escape, null!));
        running.SetResult();
        await owner.StopAsync();
    }

    private sealed class ControlledSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _pending = [];
        private readonly Lock _sync = new();
        private readonly TaskCompletionSource _posted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal Task Posted => _posted.Task;

        public override void Post(SendOrPostCallback callback, object? state)
        {
            lock (_sync)
            {
                _pending.Enqueue((callback, state));
                _ = _posted.TrySetResult();
            }
        }

        internal void ExecuteOne()
        {
            (SendOrPostCallback Callback, object? State) work;
            lock (_sync)
            {
                work = _pending.Dequeue();
            }
            work.Callback(work.State);
        }
    }
}
