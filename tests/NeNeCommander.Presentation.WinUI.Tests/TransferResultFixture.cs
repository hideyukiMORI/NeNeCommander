using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Passes controlled canonical receipts through the real gateway before rendering them.</summary>
internal static class TransferResultFixture
{
    internal static async Task<TransferResultDetail> RunAsync(OperationKind kind, ProviderStepOutcome copy, ProviderStepOutcome deletion)
    {
        QueuedFileOperationPort port = QueuedFileOperationPort.Create();
        FileSystemPath sourcePath = DualPaneTestContext.ParsePath(@"C:\left\source");
        FileSystemPath destination = DualPaneTestContext.ParsePath(@"D:\right");
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("source-original")).Identity;
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(sourcePath, identity, DeletionCapability.NotRequested)));
        port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueStep(copy);
        port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueStep(deletion);
        FileOperationRequest request = kind == OperationKind.Copy
            ? Assert.IsInstanceOfType<FileOperationRequestAccepted>(CopyRequest.Create([sourcePath], destination)).Request
            : Assert.IsInstanceOfType<FileOperationRequestAccepted>(MoveRequest.Create([sourcePath], destination)).Request;
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, new ProgressSink(), CancellationToken.None);
        return new TransferResultDetail(kind, outcome);
    }

    internal static ProviderTargetObservation Target(string kind, TransferPathObservation pathObservation)
    {
        FileSystemPath path = DualPaneTestContext.ParsePath(@"D:\right\source");
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("target-original")).Identity;
        ObservedTransferEntry entry = ObservedTransferEntry.Create(
            VerifiedEntrySnapshot.Create(path, identity, FileObjectIdentity.Create("target-object")), pathObservation);
        return kind switch
        {
            "untouched" => ProviderTargetObservation.Untouched(entry),
            "original" => ProviderTargetObservation.OriginalVerified(entry, Content()),
            "uncertain" => ProviderTargetObservation.MayHaveChanged(entry),
            "desired" => ProviderTargetObservation.DesiredVerified(entry, Content()),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    internal static TransferArtifactReceipt Artifact(TransferArtifactRole role, ArtifactContentObservation content,
        ArtifactLinkObservation link, TransferPathObservation pathObservation)
    {
        string name = role == TransferArtifactRole.OriginalBackup ? "original" : role == TransferArtifactRole.PreparedContent ? "prepared" : "recovery";
        WindowsLocalPath path = Assert.IsInstanceOfType<WindowsLocalPath>(DualPaneTestContext.ParsePath(@"D:\right\" + name));
        return TransferArtifactReceipt.Identified(role, TransferArtifactEntry.Create(path, FileObjectIdentity.Create(name),
            ArtifactFullIdentityObservation.NotObserved, pathObservation), content, link);
    }

    internal static FileContentEvidence Content()
    {
        return FileContentEvidence.Create(Assert.IsInstanceOfType<KnownEntrySize>(EntrySize.Create(17)), FileContentHash.Create(new string('A', 64)));
    }

    internal static string Localize(string key)
    {
        return key switch
        {
            "TransferObservedCurrentPath" => "current:{0}",
            "TransferObservedLastKnownPath" => "last:{0}",
            "TransferTargetUntouched" => "untouched|{0}",
            "TransferTargetOriginalVerified" => "original|{0}",
            "TransferTargetMayHaveChanged" => "uncertain|{0}",
            "TransferTargetDesiredVerified" => "desired|{0}",
            "TransferOverwriteSourceRetained" => "source-kept|{0}",
            "TransferOverwriteSourceRemoved" => "source-removed|{0}",
            "TransferOverwriteSourcePartlyRemoved" => "source-partly-removed|{0}",
            "TransferOverwriteSourceUnconfirmed" => "source-unconfirmed|{0}",
            "TransferArtifactObservedFormat" => "{0}|{1}|{2}|{3}",
            "TransferArtifactAttemptedFormat" => "{0}|unidentified|candidate:{1}",
            _ => key,
        };
    }
    private sealed class ProgressSink : IFileOperationProgressObserver
    {
        public void Report(FileOperationProgress progress)
        {
            _ = progress;
        }
    }
}
