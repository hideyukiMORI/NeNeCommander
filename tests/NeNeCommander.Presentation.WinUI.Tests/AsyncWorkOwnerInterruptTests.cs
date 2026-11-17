using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Lifecycle;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves Escape interrupt admission, completion, and ownership alongside ordinary UI work.</summary>
[TestClass]
public sealed class AsyncWorkOwnerInterruptTests
{
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
        bool first = StartInterruptOnContext(owner, context, _ =>
        {
            interrupts++;
            return interrupt.Task;
        });
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

    private static bool StartInterruptOnContext(
        AsyncWorkOwner owner,
        ControlledSynchronizationContext context,
        Func<CancellationToken, Task> interrupt)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            return owner.TryStartIntent(UserIntent.Escape, interrupt);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
