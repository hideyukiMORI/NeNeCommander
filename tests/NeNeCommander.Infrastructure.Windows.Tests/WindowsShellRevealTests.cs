using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Launching;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Launching;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves reveal shares Shell scheduling and provider policy without activating any path.</summary>
[TestClass]
public sealed class WindowsShellRevealTests
{
    /// <summary>Proves canonical files and provider roots use only the single Shell queue.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-019")]
    [DataRow("C:\\")]
    [DataRow("C:\\a & b\\item.txt")]
    [DataRow("\\\\server\\share\\")]
    [DataRow("\\\\server\\share\\a & b.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\a.txt")]
    public async Task RevealAsyncWhenAdmittedQueuesExactTargetOnShellBoundaryAsync(string path)
    {
        QueuedScheduler ordinary = new();
        QueuedScheduler shell = new();
        List<string> parses = [];
        int selections = 0;
        WindowsShellItemSelection selection = new(value =>
        {
            parses.Add(value);
            return new WindowsShellItemParseResult(0, new IntPtr(37));
        }, _ =>
        {
            selections++;
            return 0;
        }, _ => { });
        WindowsShellFileLauncher launcher = new(new WindowsLocalIoExecutionBoundary(ordinary, shell),
            _ => throw new AssertFailedException("Reveal must not activate a file."), selection);
        FileSystemPath target = Parse(path);

        Task<FileLaunchOutcome> pending = launcher.RevealAsync(target, CancellationToken.None);

        Assert.AreEqual(0, ordinary.PendingCount);
        Assert.AreEqual(1, shell.PendingCount);
        Assert.IsEmpty(parses);
        shell.Execute();
        _ = Assert.IsInstanceOfType<FileLaunchAccepted>(await pending);
        CollectionAssert.AreEqual(new[] { target.CanonicalText }, parses);
        Assert.AreEqual(1, selections);
    }

    /// <summary>Proves cancellation before scheduling performs no native work.</summary>
    [TestMethod]
    public async Task RevealAsyncWhenAlreadyCancelledDoesNotScheduleAsync()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        QueuedScheduler shell = new();
        WindowsShellFileLauncher launcher = CreateRefusingLauncher(shell);

        FileLaunchOutcome outcome = await launcher.RevealAsync(Parse("C:\\"), cancellation.Token);

        _ = Assert.IsInstanceOfType<FileLaunchCancelled>(outcome);
        Assert.AreEqual(0, shell.PendingCount);
    }

    /// <summary>Proves a queued cancellation is observed before parsing starts.</summary>
    [TestMethod]
    public async Task RevealAsyncWhenCancelledWhileQueuedDoesNotParseAsync()
    {
        using CancellationTokenSource cancellation = new();
        QueuedScheduler shell = new();
        WindowsShellFileLauncher launcher = CreateRefusingLauncher(shell);
        Task<FileLaunchOutcome> pending = launcher.RevealAsync(Parse("C:\\item.txt"), cancellation.Token);

        await cancellation.CancelAsync();
        shell.Execute();

        _ = Assert.IsInstanceOfType<FileLaunchCancelled>(await pending);
    }

    /// <summary>Proves incompatible WSL names remain rejected without fallback or scheduling.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-019")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\a.")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\NUL.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\a:b")]
    public async Task RevealAsyncWhenWslNameCannotUseShellRefusesBeforeSchedulingAsync(string path)
    {
        QueuedScheduler shell = new();
        WindowsShellFileLauncher launcher = CreateRefusingLauncher(shell);

        FileLaunchFailed failed = Assert.IsInstanceOfType<FileLaunchFailed>(
            await launcher.RevealAsync(Parse(path), CancellationToken.None));

        Assert.AreSame(FileLaunchFailureKind.ProviderUnavailable, failed.Failure);
        Assert.AreEqual(0, shell.PendingCount);
    }

    /// <summary>Proves existing UNC and WSL character limits are unchanged for reveal.</summary>
    [TestMethod]
    [DataRow("\\\\server\\share\\")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\")]
    public async Task RevealAsyncWhenPathReachesShellLimitRefusesBeforeSchedulingAsync(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        QueuedScheduler shell = new();
        WindowsShellFileLauncher launcher = CreateRefusingLauncher(shell);
        FileSystemPath target = Parse(prefix + new string('a', 260 - prefix.Length));

        FileLaunchFailed failed = Assert.IsInstanceOfType<FileLaunchFailed>(
            await launcher.RevealAsync(target, CancellationToken.None));

        Assert.AreSame(FileLaunchFailureKind.ProviderUnavailable, failed.Failure);
        Assert.AreEqual(0, shell.PendingCount);
    }

    /// <summary>Proves the real shared Shell scheduler retains STA ownership for parse and release.</summary>
    [TestMethod]
    public async Task RevealAsyncWhenUsingRealSchedulerKeepsNativeLifetimeOnSameStaAsync()
    {
        Thread? parsedOn = null;
        WindowsShellItemSelection selection = new(_ =>
        {
            parsedOn = Thread.CurrentThread;
            Assert.AreEqual(ApartmentState.STA, parsedOn.GetApartmentState());
            return new WindowsShellItemParseResult(0, new IntPtr(37));
        }, _ =>
        {
            Assert.AreSame(parsedOn, Thread.CurrentThread);
            return 0;
        }, _ => Assert.AreSame(parsedOn, Thread.CurrentThread));
        WindowsShellFileLauncher launcher = new(new WindowsLocalIoExecutionBoundary(),
            _ => throw new AssertFailedException("No activation is permitted."), selection);

        _ = Assert.IsInstanceOfType<FileLaunchAccepted>(
            await launcher.RevealAsync(Parse("C:\\item.txt"), CancellationToken.None));
    }

    private static WindowsShellFileLauncher CreateRefusingLauncher(QueuedScheduler shell)
    {
        WindowsShellItemSelection selection = new(
            _ => throw new AssertFailedException("No parse is permitted."),
            _ => throw new AssertFailedException("No selection is permitted."),
            _ => throw new AssertFailedException("No pointer is owned."));
        return new WindowsShellFileLauncher(new WindowsLocalIoExecutionBoundary(new QueuedScheduler(), shell),
            _ => throw new AssertFailedException("No activation is permitted."), selection);
    }

    private static FileSystemPath Parse(string path)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path;
    }

    private sealed class QueuedScheduler : IWindowsLocalIoScheduler
    {
        private readonly Queue<Action> _pending = new();

        internal int PendingCount => _pending.Count;

        public Task<TResult> ScheduleAsync<TResult>(Func<TResult> operation)
        {
            ArgumentNullException.ThrowIfNull(operation);
            TaskCompletionSource<TResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending.Enqueue(() => completion.SetResult(operation()));
            return completion.Task;
        }

        internal void Execute()
        {
            _pending.Dequeue()();
        }
    }
}
