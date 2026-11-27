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
        RequireStream(lease, before.Standard.EndOfFile);
        return before != WindowsFileIdentifier.ReadLocalFacts(lease.Handle)
            ? throw new IOException("The overwrite file facts changed during stream observation.")
            : TransferOverwriteFileObservation.Create(
            ObservedTransferEntry.Create(entry, TransferPathObservation.CurrentPathVerified), DirectoryEntryKind.File,
            (KnownEntrySize)EntrySize.Create(before.Standard.EndOfFile),
            (KnownEntryTimestamp)EntryTimestamp.Create(new DateTimeOffset(DateTime.FromFileTimeUtc(before.Basic.LastWriteFileTimeUtc))));
    }

    private static void RequireStream(WindowsTransferPathLease lease, long length)
    {
        try
        {
            WindowsFileIdentifier.RequireUnnamedStream(lease.Handle, length);
        }
        catch (IOException exception)
        {
            throw new WindowsOverwriteRefusalException(TransferOverwriteUnavailableReason.StreamsUnconfirmed, exception);
        }
    }

    internal static void RequireShape(WindowsLocalHandleFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        TransferOverwriteUnavailableReason? refusal = ShapeRefusal(facts);
        if (refusal is not null)
        {
            throw new WindowsOverwriteRefusalException(refusal, null);
        }
    }

    private static TransferOverwriteUnavailableReason? ShapeRefusal(WindowsLocalHandleFacts facts)
    {
        return facts switch
        {
            _ when !string.Equals(facts.FileSystemName, "NTFS", StringComparison.Ordinal)
                => TransferOverwriteUnavailableReason.UnsupportedLocation,
            _ when facts.Standard.Directory == 1 => TransferOverwriteUnavailableReason.Folder,
            _ when facts.Standard.Directory != 0 || facts.Standard.DeletePending != 0 ||
                facts.Standard.EndOfFile < 0 || facts.Standard.NumberOfLinks == 0
                => TransferOverwriteUnavailableReason.Unconfirmed,
            _ when facts.Standard.NumberOfLinks > 1 => TransferOverwriteUnavailableReason.MultipleLinks,
            _ when facts.Basic.Attributes is not ((uint)FileAttributes.Normal) and not ((uint)FileAttributes.Archive)
                => TransferOverwriteUnavailableReason.SpecialAttributes,
            _ => null,
        };
    }
}
