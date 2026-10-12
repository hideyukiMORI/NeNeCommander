using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Launching;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Launching;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the Windows Shell handoff adapter without starting an external process.</summary>
[TestClass]
public sealed class WindowsShellFileLauncherTests
{
    /// <summary>Proves the Shell receives one canonical path and its parent without command text.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-019")]
    [TestCategory("Adversarial")]
    public async Task LaunchAsyncWhenWindowsFileIsRequestedUsesFixedAssociationHandoff()
    {
        ProcessStartInfo? observed = null;
        WindowsShellFileLauncher launcher = CreateLauncher(startInfo =>
        {
            observed = startInfo;
            return null;
        });
        WindowsLocalPath target = ParseLocal("C:\\folder with space\\a&b.txt");

        FileLaunchOutcome outcome = await launcher.LaunchAsync(target, CancellationToken.None);

        _ = Assert.IsInstanceOfType<FileLaunchAccepted>(outcome);
        Assert.IsNotNull(observed);
        Assert.AreEqual(target.CanonicalText, observed.FileName);
        Assert.AreEqual("C:\\folder with space", observed.WorkingDirectory);
        Assert.IsTrue(observed.UseShellExecute);
        Assert.AreEqual(string.Empty, observed.Verb);
        Assert.AreEqual(string.Empty, observed.Arguments);
        Assert.IsEmpty(observed.ArgumentList);
    }

    /// <summary>Proves an accepted handoff disposes only the returned process wrapper.</summary>
    [TestMethod]
    [DataRow("C:\\root\\a.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\a.txt")]
    public async Task LaunchAsyncWhenStartReturnsWrapperDisposesWrapperAndAccepts(string path)
    {
        using RecordingDisposable wrapper = new();
        WindowsShellFileLauncher launcher = CreateLauncher(_ => wrapper);

        FileLaunchOutcome outcome = await launcher.LaunchAsync(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<FileLaunchAccepted>(outcome);
        Assert.IsTrue(wrapper.IsDisposed);
    }

    /// <summary>Proves each known error and the fallback remain inside the closed failure model.</summary>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(5)]
    [DataRow(31)]
    [DataRow(1155)]
    [DataRow(1234)]
    public async Task LaunchAsyncWhenShellRejectsNormalizesExpectedFailure(int nativeErrorCode)
    {
        WindowsShellFileLauncher launcher = CreateLauncher(
            _ => throw new Win32Exception(nativeErrorCode));

        FileLaunchOutcome outcome = await launcher.LaunchAsync(
            ParseLocal("C:\\root\\a.txt"),
            CancellationToken.None);

        FileLaunchFailed failed = Assert.IsInstanceOfType<FileLaunchFailed>(outcome);
        Assert.AreSame(ExpectedFailure(nativeErrorCode), failed.Failure);
    }

    /// <summary>Proves cancellation before scheduling starts neither queued work nor a handoff.</summary>
    [TestMethod]
    [DataRow("C:\\root\\a.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\a.txt")]
    public async Task LaunchAsyncWhenAlreadyCancelledDoesNotScheduleOrStart(string path)
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        ManualIoScheduler scheduler = new();
        int starts = 0;
        WindowsShellFileLauncher launcher = new(
            new WindowsLocalIoExecutionBoundary(scheduler),
            _ =>
            {
                starts++;
                return null;
            });

        FileLaunchOutcome outcome = await launcher.LaunchAsync(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path,
            cancellation.Token);

        _ = Assert.IsInstanceOfType<FileLaunchCancelled>(outcome);
        Assert.AreEqual(0, scheduler.PendingCount);
        Assert.AreEqual(0, starts);
    }

    /// <summary>Proves cancellation observed after scheduling but before handoff prevents start.</summary>
    [TestMethod]
    [DataRow("C:\\root\\a.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\a.txt")]
    public async Task LaunchAsyncWhenCancelledWhileQueuedDoesNotStart(string path)
    {
        using CancellationTokenSource cancellation = new();
        ManualIoScheduler scheduler = new();
        int starts = 0;
        WindowsShellFileLauncher launcher = new(
            new WindowsLocalIoExecutionBoundary(scheduler),
            _ =>
            {
                starts++;
                return null;
            });
        Task<FileLaunchOutcome> launch = launcher.LaunchAsync(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path,
            cancellation.Token);

        await cancellation.CancelAsync();
        scheduler.ExecuteAll();
        FileLaunchOutcome outcome = await launch;

        _ = Assert.IsInstanceOfType<FileLaunchCancelled>(outcome);
        Assert.AreEqual(0, starts);
    }

    /// <summary>Proves cancellation after the handoff starts does not relabel acceptance.</summary>
    [TestMethod]
    [DataRow("C:\\root\\a.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\a.txt")]
    public async Task LaunchAsyncWhenCancellationArrivesDuringStartReportsAcceptedHandoff(string path)
    {
        using CancellationTokenSource cancellation = new();
        WindowsShellFileLauncher launcher = CreateLauncher(_ =>
        {
            cancellation.Cancel();
            return null;
        });

        FileLaunchOutcome outcome = await launcher.LaunchAsync(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path,
            cancellation.Token);

        _ = Assert.IsInstanceOfType<FileLaunchAccepted>(outcome);
        Assert.IsTrue(cancellation.IsCancellationRequested);
    }

    /// <summary>Proves a Windows root is rejected before Shell invocation because it is not a file.</summary>
    [TestMethod]
    public async Task LaunchAsyncWhenTargetHasNoParentDoesNotStart()
    {
        int starts = 0;
        WindowsShellFileLauncher launcher = CreateLauncher(_ =>
        {
            starts++;
            return null;
        });

        FileLaunchOutcome outcome = await launcher.LaunchAsync(
            ParseLocal("C:\\"),
            CancellationToken.None);

        FileLaunchFailed failed = Assert.IsInstanceOfType<FileLaunchFailed>(outcome);
        Assert.AreSame(FileLaunchFailureKind.ProviderUnavailable, failed.Failure);
        Assert.AreEqual(0, starts);
    }

    /// <summary>Proves required adapter inputs reject defects before scheduling.</summary>
    [TestMethod]
    public void RequiredArgumentsWhenAbsentThrowArgumentNullException()
    {
        WindowsLocalIoExecutionBoundary boundary = new();
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsShellFileLauncher(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsShellFileLauncher(boundary, null!));
        WindowsShellFileLauncher launcher = CreateLauncher(_ => null);
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => launcher.LaunchAsync(null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsShellFileLauncher.CreateStartInfo(null!, ParseLocal("C:\\")));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsShellFileLauncher.CreateStartInfo(ParseLocal("C:\\a.txt"), null!));
    }

    /// <summary>Proves a WSL alias is handed off once as the exact canonical namespace and parent.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-019")]
    [TestCategory("Adversarial")]
    public async Task LaunchAsyncWhenWslFileHasShellMetacharactersPreservesOneFilenameAsync()
    {
        ProcessStartInfo? observed = null;
        int starts = 0;
        WindowsShellFileLauncher launcher = CreateLauncher(startInfo =>
        {
            starts++;
            observed = startInfo;
            return null;
        });
        FileSystemPath target = Assert.IsInstanceOfType<PathParseSuccess>(
            FileSystemPath.Parse("\\\\wsl$\\Ubuntu\\home\\Mixed Case&$(name);文.txt")).Path;

        FileLaunchOutcome outcome = await launcher.LaunchAsync(target, CancellationToken.None);

        _ = Assert.IsInstanceOfType<FileLaunchAccepted>(outcome);
        Assert.AreEqual(1, starts);
        Assert.IsNotNull(observed);
        Assert.AreEqual("\\\\wsl.localhost\\Ubuntu\\home\\Mixed Case&$(name);文.txt", observed.FileName);
        Assert.AreEqual("\\\\wsl.localhost\\Ubuntu\\home", observed.WorkingDirectory);
        Assert.IsTrue(observed.UseShellExecute);
        Assert.AreEqual(string.Empty, observed.Verb);
        Assert.AreEqual(string.Empty, observed.Arguments);
        Assert.IsEmpty(observed.ArgumentList);
    }

    /// <summary>Proves lexical and provider refusals schedule no work and never reach the Shell.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-019")]
    [TestCategory("Adversarial")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\")]
    [DataRow("\\\\server\\share\\file.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\file.")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\file ")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home.\\file.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home \\file.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\a:b.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\a\"b.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\NUL.txt")]
    public async Task LaunchAsyncWhenPathCannotUseShellRefusesBeforeSchedulingAsync(string path)
    {
        ManualIoScheduler scheduler = new();
        int starts = 0;
        WindowsShellFileLauncher launcher = new(new WindowsLocalIoExecutionBoundary(scheduler), _ =>
        {
            starts++;
            return null;
        });
        FileSystemPath target = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path;

        FileLaunchOutcome outcome = await launcher.LaunchAsync(target, CancellationToken.None);

        FileLaunchFailed failed = Assert.IsInstanceOfType<FileLaunchFailed>(outcome);
        Assert.AreSame(FileLaunchFailureKind.ProviderUnavailable, failed.Failure);
        Assert.AreEqual(0, scheduler.PendingCount);
        Assert.AreEqual(0, starts);
    }

    /// <summary>Proves the ordinary WSL Shell path boundary includes its terminating null.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-019")]
    [TestCategory("Adversarial")]
    [DataRow(259)]
    [DataRow(260)]
    public async Task LaunchAsyncWhenWslPathReachesLengthBoundaryAdmitsOnlyShorterPathAsync(int length)
    {
        const string Prefix = "\\\\wsl.localhost\\Ubuntu\\";
        FileSystemPath target = Assert.IsInstanceOfType<PathParseSuccess>(
            FileSystemPath.Parse(Prefix + new string('a', length - Prefix.Length))).Path;
        int starts = 0;
        WindowsShellFileLauncher launcher = CreateLauncher(_ =>
        {
            starts++;
            return null;
        });

        FileLaunchOutcome outcome = await launcher.LaunchAsync(target, CancellationToken.None);

        if (length == 259)
        {
            _ = Assert.IsInstanceOfType<FileLaunchAccepted>(outcome);
            Assert.AreEqual(1, starts);
        }
        else
        {
            FileLaunchFailed failed = Assert.IsInstanceOfType<FileLaunchFailed>(outcome);
            Assert.AreSame(FileLaunchFailureKind.ProviderUnavailable, failed.Failure);
            Assert.AreEqual(0, starts);
        }
    }

    private static WindowsShellFileLauncher CreateLauncher(Func<ProcessStartInfo, IDisposable?> start)
    {
        return new WindowsShellFileLauncher(new WindowsLocalIoExecutionBoundary(), start);
    }

    private static FileLaunchFailureKind ExpectedFailure(int nativeErrorCode)
    {
        return nativeErrorCode switch
        {
            2 or 3 => FileLaunchFailureKind.NotFound,
            5 => FileLaunchFailureKind.AccessDenied,
            31 or 1155 => FileLaunchFailureKind.AssociationUnavailable,
            _ => FileLaunchFailureKind.ShellRejected,
        };
    }

    private static WindowsLocalPath ParseLocal(string input)
    {
        PathParseSuccess success = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input));
        return Assert.IsInstanceOfType<WindowsLocalPath>(success.Path);
    }

    private sealed class RecordingDisposable : IDisposable
    {
        internal bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class ManualIoScheduler : IWindowsLocalIoScheduler
    {
        private readonly Queue<Action> _pending = new();

        internal int PendingCount => _pending.Count;

        internal void ExecuteAll()
        {
            while (_pending.Count > 0)
            {
                _pending.Dequeue()();
            }
        }

        public Task<TResult> ScheduleAsync<TResult>(Func<TResult> operation)
        {
            TaskCompletionSource<TResult> completion = new();
            _pending.Enqueue(() => completion.SetResult(operation()));
            return completion.Task;
        }
    }
}
