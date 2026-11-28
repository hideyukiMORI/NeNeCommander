using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Builds exact displayed conflicts through the real session and gateway before testing their pure projection.</summary>
internal static class ConflictModalFixture
{
    internal static async Task<DualPaneSnapshot> AwaitAsync(IReadOnlyList<TransferConflict> conflicts)
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right, out FileOperationGateway gateway, out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        TransferConflict[] ordered = [.. conflicts.OrderBy(conflict => conflict.Source.Path.CanonicalText, StringComparer.Ordinal)];
        string[] names = [.. ordered.Select(conflict => conflict.Source.Path.CanonicalText[(conflict.Source.Path.CanonicalText.LastIndexOf('\\') + 1)..])];
        DirectoryListing leftListing = DualPaneTestContext.CreateListing(@"C:\left", names);
        DirectoryListing rightListing = DualPaneTestContext.CreateListing(@"D:\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.SelectAll, RecordingDualPaneObserver.Create(), CancellationToken.None);
        foreach (TransferConflict conflict in ordered)
        {
            port.EnqueueInspection(FileInspectionOutcome.Succeeded(conflict.Source));
        }
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted(ordered));
        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = Assert.IsInstanceOfType<OperationAwaitingConflict>(snapshot.Operation);
        return snapshot;
    }

    internal static TransferConflict Conflict(string name, TransferOverwriteUnavailableReason? refusal)
    {
        TransferOverwriteEvidence evidence = TransferOverwriteEvidence.Create(Observation(@"C:\left\" + name), Observation(@"D:\right\" + name));
        FileEntrySnapshot source = FileEntrySnapshot.Create(evidence.Source.Observation.Entry.Path, evidence.Source.Observation.Entry.Identity, DeletionCapability.NotRequested);
        TransferOverwriteCapability overwrite = refusal is null
            ? TransferOverwriteCapability.Available(evidence) : TransferOverwriteCapability.Unavailable(refusal);
        return TransferConflict.Create(source, evidence.Target.Observation.Entry.Path, DualPaneTestContext.ParsePath(@"D:\right\" + name + " (2)"), overwrite);
    }

    internal static string Localize(string key)
    {
        return PresentationResourceCatalog.English.Get(key);
    }

    private static TransferOverwriteFileObservation Observation(string text)
    {
        FileSystemPath path = DualPaneTestContext.ParsePath(text);
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse(text)).Identity;
        ObservedTransferEntry entry = ObservedTransferEntry.Create(
            VerifiedEntrySnapshot.Create(path, identity, FileObjectIdentity.Create(text)), TransferPathObservation.CurrentPathVerified);
        return TransferOverwriteFileObservation.Create(entry, DirectoryEntryKind.File,
            Assert.IsInstanceOfType<KnownEntrySize>(EntrySize.Create(17)),
            Assert.IsInstanceOfType<KnownEntryTimestamp>(EntryTimestamp.Create(new DateTimeOffset(2026, 11, 25, 0, 0, 0, TimeSpan.Zero))));
    }
}
