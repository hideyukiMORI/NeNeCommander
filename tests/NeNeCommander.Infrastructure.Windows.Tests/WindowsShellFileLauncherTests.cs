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
    [DataRow("\\\\server\\share\\a.txt")]
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
    [DataRow("\\\\server\\share\\a.txt")]
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
    [DataRow("\\\\server\\share\\a.txt")]
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
    [DataRow("\\\\server\\share\\a.txt")]
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
    [DataRow("\\\\server\\share\\")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\file.")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\file ")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home.\\file.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home \\file.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\a:b.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\a\"b.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\NUL.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\com¹.txt")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\LPT³.tar.gz\\file.txt")]
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

    /// <summary>Proves the ordinary UNC/WSL Shell path boundary includes its terminating null.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-019")]
    [TestCategory("Adversarial")]
    [DataRow(259, "\\\\wsl.localhost\\Ubuntu\\")]
    [DataRow(260, "\\\\wsl.localhost\\Ubuntu\\")]
    [DataRow(259, "\\\\server\\share\\")]
    [DataRow(260, "\\\\server\\share\\")]
    public async Task LaunchAsyncWhenNamespacePathReachesLengthBoundaryAdmitsOnlyShorterPathAsync(int length, string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        FileSystemPath target = Assert.IsInstanceOfType<PathParseSuccess>(
            FileSystemPath.Parse(prefix + new string('a', length - prefix.Length))).Path;
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

    /// <summary>Proves UNC shares and aliases are handed off once without remapping or credential input.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-019")]
    [TestCategory("Adversarial")]
    [DataRow("\\\\Server\\Share Name\\Folder\\a&$(name);文.txt", "\\\\Server\\Share Name\\Folder")]
    [DataRow("\\\\Server\\C$\\a.txt", "\\\\Server\\C$\\")]
    [DataRow("\\\\127.0.0.1\\Share\\a.txt", "\\\\127.0.0.1\\Share\\")]
    public async Task LaunchAsyncWhenUncFileIsRequestedPreservesExactHandoffAsync(string path, string parent)
    {
        ProcessStartInfo? observed = null;
        int starts = 0;
        WindowsShellFileLauncher launcher = CreateLauncher(startInfo =>
        {
            observed = startInfo;
            starts++;
            return null;
        });
        WindowsUncPath target = Assert.IsInstanceOfType<WindowsUncPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path);

        FileLaunchOutcome outcome = await launcher.LaunchAsync(target, CancellationToken.None);

        _ = Assert.IsInstanceOfType<FileLaunchAccepted>(outcome);
        Assert.AreEqual(1, starts);
        Assert.IsNotNull(observed);
        Assert.AreEqual(path, observed.FileName);
        Assert.AreEqual(parent, observed.WorkingDirectory);
        Assert.IsTrue(observed.UseShellExecute);
        Assert.AreEqual(string.Empty, observed.Verb);
        Assert.AreEqual(string.Empty, observed.Arguments);
        Assert.IsEmpty(observed.ArgumentList);
        Assert.AreEqual(string.Empty, observed.UserName);
        Assert.AreEqual(string.Empty, observed.Domain);
        Assert.IsFalse(observed.ErrorDialog);
    }

    /// <summary>Proves network and credential failures stay typed without another Shell attempt.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-019")]
    [TestCategory("Adversarial")]
    [DataRow(53, "ProviderUnavailable")]
    [DataRow(67, "ProviderUnavailable")]
    [DataRow(59, "ProviderUnavailable")]
    [DataRow(64, "ProviderUnavailable")]
    [DataRow(121, "ProviderUnavailable")]
    [DataRow(1231, "ProviderUnavailable")]
    [DataRow(1232, "ProviderUnavailable")]
    [DataRow(1326, "AccessDenied")]
    [DataRow(1219, "AccessDenied")]
    public async Task LaunchAsyncWhenUncSessionFailsNormalizesWithoutRetryAsync(int nativeErrorCode, string expected)
    {
        const string Path = "\\\\server\\share\\a.txt";
        int starts = 0;
        WindowsShellFileLauncher launcher = CreateLauncher(startInfo =>
        {
            starts++;
            Assert.AreEqual(Path, startInfo.FileName);
            throw new Win32Exception(nativeErrorCode);
        });
        FileSystemPath target = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(Path)).Path;

        FileLaunchOutcome outcome = await launcher.LaunchAsync(target, CancellationToken.None);

        FileLaunchFailed failed = Assert.IsInstanceOfType<FileLaunchFailed>(outcome);
        Assert.AreSame(
            expected == "AccessDenied" ? FileLaunchFailureKind.AccessDenied : FileLaunchFailureKind.ProviderUnavailable,
            failed.Failure);
        Assert.AreEqual(1, starts);
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
