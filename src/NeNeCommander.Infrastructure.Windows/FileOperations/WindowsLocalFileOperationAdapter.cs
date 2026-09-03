using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Implements the sole file-operation port for Windows local paths. Every mutation revalidates
/// the preflighted identity first, never follows reparse points, reports only permanent deletion
/// because no recycle mechanism exists yet, and normalizes platform failures to the canonical vocabulary.
/// An absent snapshot is rejected by <see cref="WindowsLocalEntryIdentity.Revalidate"/> before any step runs.
/// </summary>
public sealed class WindowsLocalFileOperationAdapter : IFileOperationPort
{
    /// <inheritdoc />
    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Task.FromResult(Inspect(path));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        return Task.FromResult(Guarded(() => Preflight(sources, destination), FileOperationFailureKind.Inspection));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return Task.FromResult(Guarded(() => Copy(source, destination), FileOperationFailureKind.Copy));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return Task.FromResult(Guarded(() => Verify(source, destination), FileOperationFailureKind.Verification));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mode);
        return Task.FromResult(Guarded(() => Delete(source, mode), FileOperationFailureKind.Delete));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        return Task.FromResult(Guarded(() => CreateDirectory(location, target), FileOperationFailureKind.ProviderUnavailable));
    }

    private static ProviderStepOutcome CreateDirectory(FileEntrySnapshot location, FileSystemPath target)
    {
        return target is not WindowsLocalPath localTarget
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(location, entry => CreateDirectoryBeneath(entry, location.Path, localTarget));
    }

    private static ProviderStepOutcome CreateDirectoryBeneath(FileSystemInfo entry, FileSystemPath location, WindowsLocalPath target)
    {
        if (entry is not DirectoryInfo)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.NotFound);
        }
        if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (ProviderPathContainment.Evaluate(location, target) is not ContainedPath)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Inspection);
        }
        if (TargetExists(target.CanonicalText))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        _ = Directory.CreateDirectory(target.CanonicalText);
        return ProviderStepOutcome.Succeeded();
    }

    private static FileInspectionOutcome Inspect(FileSystemPath path)
    {
        if (path is not WindowsLocalPath local)
        {
            return FileInspectionOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        try
        {
            FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(local);
            return entry is null
                ? FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound)
                : FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(
                    path,
                    WindowsLocalEntryIdentity.Describe(entry),
                    DeletionCapability.PermanentOnly));
        }
        catch (UnauthorizedAccessException exception)
        {
            return FileInspectionOutcome.Failed(Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
        catch (IOException exception)
        {
            return FileInspectionOutcome.Failed(Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
    }

    private static ProviderStepOutcome Preflight(IReadOnlyList<FileEntrySnapshot> sources, FileSystemPath destination)
    {
        if (destination is not WindowsLocalPath localDestination)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (!new DirectoryInfo(localDestination.CanonicalText).Exists)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.NotFound);
        }
        foreach (FileEntrySnapshot source in sources)
        {
            ProviderStepOutcome sourceOutcome = WithRevalidatedEntry(source, entry =>
                ProviderPathContainment.Evaluate(source.Path, destination) is ContainedPath ||
                TargetExists(BuildTargetText(localDestination, entry))
                    ? ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict)
                    : ProviderStepOutcome.Succeeded());
            if (sourceOutcome.Failure is not null)
            {
                return sourceOutcome;
            }
        }
        return ProviderStepOutcome.Succeeded();
    }

    private static ProviderStepOutcome Copy(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WindowsLocalPath localDestination
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, entry => CopyEntry(entry, BuildTargetText(localDestination, entry)));
    }

    private static ProviderStepOutcome CopyEntry(FileSystemInfo entry, string targetText)
    {
        if (TargetExists(targetText))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (WindowsLocalTreeCopy.ContainsReparsePoint(entry))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        WindowsLocalTreeCopy.Copy(entry, targetText);
        return ProviderStepOutcome.Succeeded();
    }

    private static ProviderStepOutcome Verify(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WindowsLocalPath localDestination
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, entry =>
                WindowsLocalTreeCopy.Matches(entry, BuildTargetText(localDestination, entry))
                    ? ProviderStepOutcome.Succeeded()
                    : ProviderStepOutcome.Failed(FileOperationFailureKind.Verification));
    }

    private static ProviderStepOutcome Delete(FileEntrySnapshot source, DeletionExecutionMode mode)
    {
        return mode != DeletionExecutionMode.Permanent
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WithRevalidatedEntry(source, DeleteEntry);
    }

    private static ProviderStepOutcome DeleteEntry(FileSystemInfo entry)
    {
        if (entry is DirectoryInfo directory)
        {
            directory.Delete(recursive: true);
        }
        else
        {
            entry.Delete();
        }
        return ProviderStepOutcome.Succeeded();
    }

    private static ProviderStepOutcome WithRevalidatedEntry(
        FileEntrySnapshot source,
        Func<FileSystemInfo, ProviderStepOutcome> step)
    {
        return WindowsLocalEntryIdentity.Revalidate(source) switch
        {
            EntryRejected rejected => ProviderStepOutcome.Failed(rejected.Failure),
            EntryMatched matched => step(matched.Entry),
            _ => throw new InvalidOperationException("The revalidation outcome variant is not executable."),
        };
    }

    private static ProviderStepOutcome Guarded(Func<ProviderStepOutcome> step, FileOperationFailureKind fallback)
    {
        try
        {
            return step();
        }
        catch (UnauthorizedAccessException exception)
        {
            return ProviderStepOutcome.Failed(Normalize(exception.HResult, fallback));
        }
        catch (IOException exception)
        {
            return ProviderStepOutcome.Failed(Normalize(exception.HResult, fallback));
        }
    }

    internal static FileOperationFailureKind Normalize(int hResult, FileOperationFailureKind fallback)
    {
        FileOperationFailureKind normalized = WindowsFileFailureNormalizer.Normalize(hResult);
        return normalized == FileOperationFailureKind.ProviderUnavailable ? fallback : normalized;
    }

    private static string BuildTargetText(WindowsLocalPath destination, FileSystemInfo entry)
    {
        return Path.Combine(destination.CanonicalText, entry.Name);
    }

    private static bool TargetExists(string targetText)
    {
        return File.Exists(targetText) || Directory.Exists(targetText);
    }
}
