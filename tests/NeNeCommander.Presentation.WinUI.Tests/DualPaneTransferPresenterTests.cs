using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves DualPaneTransferPresenter behavior and contracts.</summary>
[TestClass]
public sealed class DualPaneTransferPresenterTests
{
    /// <summary>Proves a request the gateway never receives is shown as a rejected request.</summary>
    [TestMethod]
    public async Task PresentWhenMoveRequestIsRejectedShowsRequestRejectedStatus()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing("C:\\", ["Users"]), null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing("C:\\Users", []), null));
        _ = await panes.NavigateAsync(PaneSide.Left, DualPaneTestContext.ParsePath("C:\\"), CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, DualPaneTestContext.ParsePath("C:\\Users"), CancellationToken.None);

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        DualPanePresentation rejected = DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc, KeyHintVisibility.Shown);

        Assert.AreSame(OperationStatus.MoveRequestRejected, rejected.OperationStatus);
        Assert.AreSame(OperationBarTone.Failure, rejected.Tone);
    }

    /// <summary>Proves each gateway completion and the running state map to one operation status.</summary>
    [TestMethod]
    public async Task PresentWhenMoveCompletesTranslatesEachCompletionKind()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Assert.AreSame(OperationStatus.MoveSucceeded, await MoveStatusAsync(port =>
        {
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        }, CancellationToken.None));
        Assert.AreSame(OperationStatus.MoveRejected, await MoveStatusAsync(port =>
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Conflict), ProviderStepReceipt.Empty)), CancellationToken.None));
        Assert.AreSame(OperationStatus.MovePartiallyCompleted, await MoveStatusAsync(port =>
        {
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Verification), ProviderStepReceipt.Empty));
        }, CancellationToken.None));
        Assert.AreSame(OperationStatus.MoveCancelled, await MoveStatusAsync(port => { }, cancellation.Token));
    }

    private static async Task<OperationStatus> MoveStatusAsync(
        Action<QueuedFileOperationPort> script,
        CancellationToken cancellationToken)
    {
        return (await MovePresentationAsync(script, cancellationToken)).OperationStatus;
    }

    private static async Task<DualPanePresentation> MovePresentationAsync(
        Action<QueuedFileOperationPort> script,
        CancellationToken cancellationToken)
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = DualPaneTestContext.CreateListing("C:\\left", ["a.txt"]);
        DirectoryListing rightListing = DualPaneTestContext.CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(leftListing.Entries[0].Path, identity.Identity, DeletionCapability.Recycle)));
        script(port);

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), cancellationToken);

        return DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc, KeyHintVisibility.Shown);
    }

    /// <summary>Proves each copy completion maps to one copy status and a rejected copy request is named.</summary>
    [TestMethod]
    public async Task PresentWhenCopyCompletesTranslatesEachCompletionKind()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Assert.AreSame(OperationStatus.CopySucceeded, await OperationStatusAsync(UserIntent.Copy, port =>
        {
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        }, CancellationToken.None));
        Assert.AreSame(OperationStatus.CopyRejected, await OperationStatusAsync(UserIntent.Copy, port =>
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Conflict), ProviderStepReceipt.Empty)), CancellationToken.None));
        Assert.AreSame(OperationStatus.CopyPartiallyCompleted, await OperationStatusAsync(UserIntent.Copy, port =>
        {
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Verification), ProviderStepReceipt.Empty));
        }, CancellationToken.None));
        Assert.AreSame(OperationStatus.CopyCancelled, await OperationStatusAsync(UserIntent.Copy, port => { }, cancellation.Token));
    }

    /// <summary>Proves a copy request the gateway never receives is shown as a rejected copy request.</summary>
    [TestMethod]
    public async Task PresentWhenCopyRequestIsRejectedShowsCopyRequestRejectedStatus()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing("C:\\", ["Users"]), null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing("C:\\Users", []), null));
        _ = await panes.NavigateAsync(PaneSide.Left, DualPaneTestContext.ParsePath("C:\\"), CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, DualPaneTestContext.ParsePath("C:\\Users"), CancellationToken.None);

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(OperationStatus.CopyRequestRejected, DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc, KeyHintVisibility.Shown).OperationStatus);
    }

    private static async Task<OperationStatus> OperationStatusAsync(
        UserIntent intent,
        Action<QueuedFileOperationPort> script,
        CancellationToken cancellationToken)
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = DualPaneTestContext.CreateListing("C:\\left", ["a.txt"]);
        DirectoryListing rightListing = DualPaneTestContext.CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(leftListing.Entries[0].Path, identity.Identity, DeletionCapability.Recycle)));
        script(port);

        DualPaneSnapshot snapshot = await panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), cancellationToken);

        return DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc, KeyHintVisibility.Shown).OperationStatus;
    }

    /// <summary>Proves a completed, cancelled, or idle operation keeps the idle bar tone.</summary>
    [TestMethod]
    public async Task PresentWhenOperationEndsWithoutFailureKeepsTheIdleTone()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        DualPanePresentation succeeded = await MovePresentationAsync(port =>
        {
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        }, CancellationToken.None);
        DualPanePresentation cancelled = await MovePresentationAsync(port => { }, cancellation.Token);
        DualPanePresentation rejected = await MovePresentationAsync(port =>
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Conflict), ProviderStepReceipt.Empty)), CancellationToken.None);

        Assert.AreSame(OperationBarTone.Idle, succeeded.Tone);
        TransferResultDetail result = Assert.IsInstanceOfType<TransferResultDetail>(succeeded.Detail);
        Assert.AreEqual(0, result.NotTransferred);
        Assert.AreEqual(1, result.Copied);
        Assert.AreEqual(1, result.Verified);
        Assert.AreEqual(1, result.SourceDeleted);
        Assert.AreSame(OperationBarTone.Idle, cancelled.Tone);
        Assert.AreSame(OperationBarTone.Failure, rejected.Tone);
    }

    /// <summary>Proves partial source removal is visible separately from an entirely deleted source.</summary>
    [TestMethod]
    public async Task PresentWhenMoveSourceIsPartiallyDeletedShowsDistinctRemovalCountAndFailure()
    {
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([ProviderStepEffectKind.SourcePartiallyDeleted], null, []);
        DualPanePresentation presentation = await MovePresentationAsync(port =>
        {
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Delete), receipt));
        }, CancellationToken.None);

        TransferResultDetail result = Assert.IsInstanceOfType<TransferResultDetail>(presentation.Detail);
        Assert.AreEqual(0, result.SourceDeleted);
        Assert.AreEqual(1, result.SourcePartiallyDeleted);
        Assert.HasCount(2, result.ProviderSteps);
        Assert.AreSame(receipt, result.ProviderSteps[1].Receipt);
        Assert.AreEqual(1, result.Copied);
        Assert.AreEqual(1, result.Verified);
        Assert.AreSame(OperationStatus.MovePartiallyCompleted, presentation.OperationStatus);
        Assert.AreSame(OperationBarTone.Failure, presentation.Tone);
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SourceDeletionCounts.Create(0, -1));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new TransferResultDetail(null!));
    }

    /// <summary>Proves failed preparation reaches the existing result presentation without inventing copied or verified counts.</summary>
    [TestMethod]
    [DataRow("failure", "unidentified")]
    [DataRow("cancelled", "unidentified")]
    [DataRow("failure", "stableOnly")]
    [DataRow("cancelled", "stableOnly")]
    [DataRow("failure", "full")]
    [DataRow("cancelled", "full")]
    public async Task PresentWhenPreparationStopsKeepsUntouchedTargetAndArtifactKnowledge(string completion, string knowledge)
    {
        NeNeCommander.Domain.Paths.FileSystemPath targetPath = DualPaneTestContext.ParsePath("C:\\right\\a.txt");
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("target-full")).Identity;
        ObservedTransferEntry target = ObservedTransferEntry.Create(
            VerifiedEntrySnapshot.Create(targetPath, identity, FileObjectIdentity.Create("target-object")),
            TransferPathObservation.LastKnownPath);
        TransferArtifactReceipt artifact = PreparationArtifact(knowledge);
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], ProviderTargetObservation.Untouched(target), [artifact]);

        ProviderStepCompletion providerCompletion = completion == "cancelled"
            ? ProviderStepCompletion.Cancelled : ProviderStepCompletion.Failed(FileOperationFailureKind.Copy);
        DualPanePresentation presentation = await MovePresentationAsync(port =>
        {
            port.EnqueueStep(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueStep(ProviderStepOutcome.Create(providerCompletion, receipt));
        }, CancellationToken.None);

        TransferResultDetail detail = Assert.IsInstanceOfType<TransferResultDetail>(presentation.Detail);
        Assert.AreSame(completion == "cancelled" ? OperationStatus.MoveCancelled : OperationStatus.MovePartiallyCompleted, presentation.OperationStatus);
        Assert.AreSame(completion == "cancelled" ? OperationBarTone.Idle : OperationBarTone.Failure, presentation.Tone);
        Assert.AreEqual(0, detail.Copied);
        Assert.AreEqual(0, detail.Verified);
        Assert.AreEqual(0, detail.SourceDeleted);
        Assert.HasCount(1, detail.ProviderSteps);
        Assert.AreSame(receipt, detail.ProviderSteps[0].Receipt);
        Assert.AreSame(target, detail.ProviderSteps[0].Receipt.Target!.Target);
        Assert.AreSame(artifact, detail.ProviderSteps[0].Receipt.Artifacts[0]);
    }

    private static TransferArtifactReceipt PreparationArtifact(string knowledge)
    {
        WindowsLocalPath path = Assert.IsInstanceOfType<WindowsLocalPath>(DualPaneTestContext.ParsePath("C:\\right\\backup"));
        if (knowledge == "unidentified")
        {
            return TransferArtifactReceipt.Unidentified(TransferArtifactRole.OriginalBackup, path);
        }
        ArtifactFullIdentityObservation fullIdentity = knowledge == "stableOnly"
            ? ArtifactFullIdentityObservation.NotObserved
            : ArtifactFullIdentityObservation.Observed(Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("artifact-full")).Identity);
        return TransferArtifactReceipt.Identified(TransferArtifactRole.OriginalBackup,
            TransferArtifactEntry.Create(path, FileObjectIdentity.Create("artifact-object"), fullIdentity, TransferPathObservation.LastKnownPath),
            ArtifactContentObservation.Unknown, ArtifactLinkObservation.Retained);
    }

}
