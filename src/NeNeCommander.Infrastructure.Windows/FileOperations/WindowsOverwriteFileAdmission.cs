using System;
using System.IO;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Qualifies sequential same-handle observations without granting a permanent metadata, alias or stream freeze.</summary>
internal static class WindowsOverwriteFileAdmission
{
    internal static TransferOverwriteFileObservation Observe(WindowsTransferPathLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        TransferOverwriteFileObservation observation = ObserveCore(lease, lease.Revalidate);
        return observation.Observation.Entry != lease.Baseline
            ? throw new IOException("The original overwrite file identity changed.") : observation;
    }

    /// <summary>Admits current post-effect facts for the same held object; it does not refresh original confirmation or prove byte equality.</summary>
    internal static TransferOverwriteFileObservation ObserveAfterContentChange(WindowsTransferPathLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        return ObserveCore(lease, lease.RevalidateObject);
    }

    private static TransferOverwriteFileObservation ObserveCore(WindowsTransferPathLease lease, Action revalidate)
    {
        revalidate();
        WindowsLocalHandleFacts before = WindowsFileIdentifier.ReadLocalFacts(lease.Handle);
        RequireShape(before);
        VerifiedEntrySnapshot entry = WindowsLocalEntryIdentity.CaptureHeld(lease.Path, before);
        revalidate();
        WindowsFileIdentifier.RequireUnnamedStream(lease.Handle, before.Standard.EndOfFile);
        return before != WindowsFileIdentifier.ReadLocalFacts(lease.Handle)
            ? throw new IOException("The overwrite file facts changed during stream observation.")
            : TransferOverwriteFileObservation.Create(
            ObservedTransferEntry.Create(entry, TransferPathObservation.CurrentPathVerified), DirectoryEntryKind.File,
            (KnownEntrySize)EntrySize.Create(before.Standard.EndOfFile),
            (KnownEntryTimestamp)EntryTimestamp.Create(new DateTimeOffset(DateTime.FromFileTimeUtc(before.Basic.LastWriteFileTimeUtc))));
    }

    internal static void RequireShape(WindowsLocalHandleFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!string.Equals(facts.FileSystemName, "NTFS", StringComparison.Ordinal) ||
            facts.Standard.Directory != 0 || facts.Standard.DeletePending != 0 ||
            facts.Standard.NumberOfLinks != 1 || facts.Standard.EndOfFile < 0 ||
            facts.Basic.Attributes is not ((uint)FileAttributes.Normal) and not ((uint)FileAttributes.Archive))
        {
            throw new IOException("The overwrite file is not a single-link regular NTFS file with admitted attributes.");
        }
    }
}
