using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Launching;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.Launching;

/// <summary>
/// Hands an admitted Windows-local or WSL file path to its Windows Shell association without
/// exposing shell verbs, arguments, or external process lifetime to callers.
/// </summary>
public sealed class WindowsShellFileLauncher : IFileLauncher
{
    private const int ErrorAccessDenied = 5;
    private const int ErrorFileNotFound = 2;
    private const int ErrorNoAssociation = 1155;
    private const int ErrorPathNotFound = 3;
    private const int ShellErrorNoAssociation = 31;
    private const int ShellPathLengthLimit = 260;

    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;
    private readonly Func<ProcessStartInfo, IDisposable?> _start;

    /// <summary>Initializes a launcher scheduled through the shared Windows-local boundary.</summary>
    /// <param name="executionBoundary">Shared scheduler for synchronous Windows-side work.</param>
    public WindowsShellFileLauncher(WindowsLocalIoExecutionBoundary executionBoundary)
        : this(executionBoundary, static startInfo => Process.Start(startInfo))
    {
    }

    internal WindowsShellFileLauncher(
        WindowsLocalIoExecutionBoundary executionBoundary,
        Func<ProcessStartInfo, IDisposable?> start)
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        ArgumentNullException.ThrowIfNull(start);
        _executionBoundary = executionBoundary;
        _start = start;
    }

    /// <inheritdoc />
    public Task<FileLaunchOutcome> LaunchAsync(
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        return cancellationToken.IsCancellationRequested
            ? Task.FromResult(FileLaunchOutcome.Cancelled())
            : IsSupportedTarget(target) && target.Parent is FileSystemPath parent
                ? _executionBoundary.ExecuteAsync(() => Launch(target, parent, cancellationToken))
                : Task.FromResult(FileLaunchOutcome.Failed(FileLaunchFailureKind.ProviderUnavailable));
    }

    internal static ProcessStartInfo CreateStartInfo(
        FileSystemPath target,
        FileSystemPath workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(workingDirectory);
        return new ProcessStartInfo
        {
            FileName = target.CanonicalText,
            UseShellExecute = true,
            WorkingDirectory = workingDirectory.CanonicalText,
        };
    }

    internal static FileLaunchFailureKind NormalizeFailure(int nativeErrorCode)
    {
        return nativeErrorCode switch
        {
            ErrorFileNotFound or ErrorPathNotFound => FileLaunchFailureKind.NotFound,
            ErrorAccessDenied => FileLaunchFailureKind.AccessDenied,
            ErrorNoAssociation or ShellErrorNoAssociation => FileLaunchFailureKind.AssociationUnavailable,
            _ => FileLaunchFailureKind.ShellRejected,
        };
    }

    private FileLaunchOutcome Launch(
        FileSystemPath target,
        FileSystemPath workingDirectory,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return FileLaunchOutcome.Cancelled();
        }
        ProcessStartInfo startInfo = CreateStartInfo(target, workingDirectory);
        try
        {
            using IDisposable? process = _start(startInfo);
            return FileLaunchOutcome.Accepted();
        }
        catch (Win32Exception exception)
        {
            return FileLaunchOutcome.Failed(NormalizeFailure(exception.NativeErrorCode));
        }
    }

    private static bool IsSupportedTarget(FileSystemPath target)
    {
        return target switch
        {
            WindowsLocalPath => true,
            WslPath wsl => wsl.CanonicalText.Length < ShellPathLengthLimit && wsl.HasWindowsCompatibleNames,
            _ => false,
        };
    }
}
