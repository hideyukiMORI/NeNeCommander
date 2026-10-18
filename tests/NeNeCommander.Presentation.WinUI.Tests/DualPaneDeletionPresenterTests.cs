using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves DualPaneDeletionPresenter behavior and contracts.</summary>
[TestClass]
public sealed class DualPaneDeletionPresenterTests
{
    /// <summary>Proves a pending confirmation is projected as modal input with its item count.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task PresentWhenDeleteAwaitsConfirmationReportsModalContextAndCount()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = DualPaneTestContext.CreateListing("C:\\left", ["a.txt", "b.txt"]);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing("C:\\right", []), null));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, DualPaneTestContext.ParsePath("C:\\right"), CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        foreach (DirectoryEntry entry in leftListing.Entries)
        {
            port.EnqueueInspection(FileInspectionOutcome.Succeeded(
                FileEntrySnapshot.Create(entry.Path, identity.Identity, DeletionCapability.PermanentOnly)));
        }

        DualPanePresentation pending = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None), TestMetadataFormats.Utc);
        DualPanePresentation escaped = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None), TestMetadataFormats.Utc);

        AssertPendingDeletion(pending);
        AssertEscapedDeletion(escaped);
    }

    /// <summary>Proves each delete completion and the running state map to one operation status.</summary>
    [TestMethod]
    public async Task PresentWhenDeleteCompletesTranslatesEachCompletionKind()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Assert.AreSame(OperationStatus.DeleteSucceeded, await DeleteStatusAsync(port =>
            port.EnqueueStep(ProviderStepOutcome.Succeeded()), CancellationToken.None));
        Assert.AreSame(OperationStatus.DeleteRejected, await DeleteStatusAsync(port =>
            port.EnqueueStep(ProviderStepOutcome.Failed(FileOperationFailureKind.AccessDenied)), CancellationToken.None));
        Assert.AreSame(OperationStatus.DeleteCancelled, await DeleteStatusAsync(port => { }, cancellation.Token));
    }

    /// <summary>Proves a partially completed deletion and the running state map to their statuses.</summary>
    [TestMethod]
    public async Task PresentWhenDeleteStopsAfterPartialCompletionReportsPartialStatus()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = DualPaneTestContext.CreateListing("C:\\left", ["a.txt", "b.txt"]);
        DirectoryListing rightListing = DualPaneTestContext.CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        foreach (DirectoryEntry entry in leftListing.Entries)
        {
            port.EnqueueInspection(FileInspectionOutcome.Succeeded(
                FileEntrySnapshot.Create(entry.Path, identity.Identity, DeletionCapability.Recycle)));
        }
        port.EnqueueStep(ProviderStepOutcome.Succeeded());
        port.EnqueueStep(ProviderStepOutcome.Failed(FileOperationFailureKind.Delete));

        DualPanePresentation presentation = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None), TestMetadataFormats.Utc);

        Assert.AreSame(OperationStatus.DeletePartiallyCompleted, presentation.OperationStatus);
        Assert.AreEqual(string.Empty, presentation.DeletionConfirmationAutomationId);
        Assert.AreSame(OperationBarTone.Failure, presentation.Tone);
    }

    private static async Task<OperationStatus> DeleteStatusAsync(
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

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), cancellationToken);

        return DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc).OperationStatus;
    }
    private static void AssertPendingDeletion(DualPanePresentation pending)
    {
        Assert.AreSame(OperationStatus.DeleteAwaitingConfirmation, pending.OperationStatus);
        Assert.AreEqual("DeletionConfirmation", pending.DeletionConfirmationAutomationId);
        Assert.AreEqual(2, Assert.IsInstanceOfType<OperationItemCountDetail>(pending.Detail).Count);
        Assert.AreSame(KeyboardContext.Modal, pending.InputContext);
        Assert.AreSame(OperationBarTone.AwaitingConfirmation, pending.Tone);
        Assert.HasCount(2, pending.KeyHints);
        Assert.AreEqual("KeyLabelEnter", pending.KeyHints[0].KeyLabelResourceKey);
    }

    private static void AssertEscapedDeletion(DualPanePresentation escaped)
    {
        Assert.AreEqual(string.Empty, escaped.DeletionConfirmationAutomationId);
        Assert.AreSame(OperationBarTone.Idle, escaped.Tone);
        Assert.HasCount(11, escaped.KeyHints);
        Assert.AreEqual("KeyLabelF2", escaped.KeyHints[0].KeyLabelResourceKey);
        Assert.AreEqual("KeyLabelCtrlP", escaped.KeyHints[7].KeyLabelResourceKey);
        Assert.AreEqual("KeyLabelCtrlB", escaped.KeyHints[8].KeyLabelResourceKey);
        Assert.AreEqual("IntentLabelOpenBookmarks", escaped.KeyHints[8].IntentLabelResourceKey);
        Assert.AreEqual("KeyLabelCtrlComma", escaped.KeyHints[9].KeyLabelResourceKey);
        Assert.AreEqual("KeyLabelEscape", escaped.KeyHints[10].KeyLabelResourceKey);
        Assert.AreSame(OperationStatus.Idle, escaped.OperationStatus);
        Assert.AreSame(OperationDetail.None, escaped.Detail);
        Assert.AreSame(KeyboardContext.FileList, escaped.InputContext);
    }
}
