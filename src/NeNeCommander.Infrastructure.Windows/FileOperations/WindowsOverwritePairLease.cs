using System;
using System.IO;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns exact source and target handle chains and never replaces original consent with fresh observations.</summary>
internal sealed class WindowsOverwritePairLease : IDisposable
{
    private WindowsOverwritePairLease(WindowsTransferPathLease source, WindowsTransferPathLease target, TransferOverwriteEvidence evidence)
    {
        Source = source;
        Target = target;
        Evidence = evidence;
    }

    internal WindowsTransferPathLease Source { get; }
    internal WindowsTransferPathLease Target { get; }
    internal TransferOverwriteEvidence Evidence { get; }

    internal static WindowsOverwritePairLease Open(FileEntrySnapshot source, WindowsLocalPath target, WindowsDriveBoundary driveBoundary)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(driveBoundary);
        WindowsLocalPath sourcePath = RequirePaths(source.Path, target);
        WindowsTransferPathLease? sourceLease = WindowsTransferPathLease.Open(sourcePath, FileAccess.Read, driveBoundary);
        WindowsTransferPathLease? targetLease = null;
        try
        {
            targetLease = WindowsTransferPathLease.Open(target, FileAccess.ReadWrite, driveBoundary);
            TransferOverwriteEvidence evidence = ReadEvidence(sourceLease, targetLease);
            if (evidence.Source.Observation.Entry.Identity != source.Identity)
            {
                throw new IOException("The frozen overwrite source identity changed.");
            }
            WindowsOverwritePairLease pair = new(sourceLease, targetLease, evidence);
            sourceLease = null;
            targetLease = null;
            return pair;
        }
        finally
        {
            targetLease?.Dispose();
            sourceLease?.Dispose();
        }
    }

    /// <summary>Requires untouched original observations before effects; not a post-write verification route.</summary>
    internal void Revalidate(TransferOverwriteEvidence original)
    {
        ArgumentNullException.ThrowIfNull(original);
        TransferOverwriteEvidence current = ReadEvidence(Source, Target);
        if (current != Evidence || current != original)
        {
            throw new IOException("The original overwrite confirmation no longer matches the held files.");
        }
    }

    private static WindowsLocalPath RequirePaths(FileSystemPath source, WindowsLocalPath target)
    {
        return source is WindowsLocalPath local && local.Parent is not null && target.Parent is not null &&
            !FileSystemPathIdentityComparer.Instance.Equals(source, target)
            ? local
            : throw new IOException("Overwrite requires two distinct non-root Windows local files.");
    }

    private static TransferOverwriteEvidence ReadEvidence(WindowsTransferPathLease source, WindowsTransferPathLease target)
    {
        TransferOverwriteFileObservation sourceObservation = WindowsOverwriteFileAdmission.Observe(source);
        TransferOverwriteFileObservation targetObservation = WindowsOverwriteFileAdmission.Observe(target);
        return sourceObservation != WindowsOverwriteFileAdmission.Observe(source)
            ? throw new IOException("The original overwrite source changed during target observation.")
            : sourceObservation.Observation.Entry.ObjectIdentity == targetObservation.Observation.Entry.ObjectIdentity
                ? throw new WindowsOverwriteRefusalException(TransferOverwriteUnavailableReason.SameObject, null)
                : TransferOverwriteEvidence.Create(sourceObservation, targetObservation);
    }

    public void Dispose()
    {
        Target.Dispose();
        Source.Dispose();
    }
}
