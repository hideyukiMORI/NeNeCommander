using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves DualPaneConflictPresenter behavior and contracts.</summary>
[TestClass]
public sealed class DualPaneConflictPresenterTests
{
    /// <summary>Proves conflict presentation keeps the existing modal context and exposes the safe candidate.</summary>
    [TestMethod]
    public async Task PresentWhenCopyAwaitsConflictShowsCandidateWithCancelAsInitialFocus()
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
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity")).Identity;
        FileEntrySnapshot source = FileEntrySnapshot.Create(
            leftListing.Entries[0].Path,
            identity,
            DeletionCapability.PermanentOnly);
        FileSystemPath target = Assert.IsInstanceOfType<PathParseSuccess>(rightListing.Location.Child("a.txt")).Path;
        FileSystemPath candidate = Assert.IsInstanceOfType<PathParseSuccess>(rightListing.Location.Child("a (2).txt")).Path;
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([
            TransferConflict.Create(source, target, candidate)]));

        DualPaneSnapshot snapshot = await panes.HandleAsync(
            UserIntent.Copy,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);
        DualPanePresentation presentation = DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc);

        AssertPendingConflict(presentation, source, target, candidate);
    }

    /// <summary>Proves a candidate race creates a new modal transition while repeated rendering stays stable.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task PresentWhenConflictReoccursReplacesModalWithLatestCandidate()
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
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity")).Identity;
        FileEntrySnapshot source = FileEntrySnapshot.Create(
            leftListing.Entries[0].Path,
            identity,
            DeletionCapability.PermanentOnly);
        FileSystemPath target = Assert.IsInstanceOfType<PathParseSuccess>(rightListing.Location.Child("a.txt")).Path;
        FileSystemPath firstCandidate = Assert.IsInstanceOfType<PathParseSuccess>(
            rightListing.Location.Child("a (2).txt")).Path;
        FileSystemPath racedCandidate = Assert.IsInstanceOfType<PathParseSuccess>(
            rightListing.Location.Child("a (3).txt")).Path;
        TransferConflict firstConflict = TransferConflict.Create(source, target, firstCandidate);
        EnqueueConflictRace(port, firstConflict, racedCandidate);

        DualPaneSnapshot firstSnapshot = await panes.HandleAsync(
            UserIntent.Copy,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);
        DualPanePresentation first = DualPanePresenter.Present(firstSnapshot, TestMetadataFormats.Utc);
        DualPanePresentation repeated = DualPanePresenter.Present(firstSnapshot, TestMetadataFormats.Utc, first);
        DualPaneSnapshot racedSnapshot = await panes.HandleAsync(
            UserIntent.ResolveConflict(TransferConflictDecision.KeepBoth, TransferConflictScope.All),
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);
        DualPanePresentation raced = DualPanePresenter.Present(racedSnapshot, TestMetadataFormats.Utc, repeated);

        AssertConflictReplaced(first, repeated, raced, racedCandidate);
    }

    private static void AssertConflictReplaced(
        DualPanePresentation first,
        DualPanePresentation repeated,
        DualPanePresentation raced,
        FileSystemPath racedCandidate)
    {
        Assert.AreSame(first.ConflictModal, repeated.ConflictModal);
        Assert.AreNotSame(repeated.ConflictModal, raced.ConflictModal);
        ActiveConflictModal modal = Assert.IsInstanceOfType<ActiveConflictModal>(raced.ConflictModal);
        Assert.AreEqual(racedCandidate.CanonicalText, modal.KeepBothCandidateText);
        Assert.AreSame(TransferConflictDecision.Cancel, modal.InitialFocus);
    }
    private static void AssertPendingConflict(
        DualPanePresentation presentation,
        FileEntrySnapshot source,
        FileSystemPath target,
        FileSystemPath candidate)
    {
        ActiveConflictModal modal = Assert.IsInstanceOfType<ActiveConflictModal>(presentation.ConflictModal);
        Assert.AreSame(TransferConflictDecision.Cancel, modal.InitialFocus);
        Assert.AreEqual(source.Path.CanonicalText, modal.SourceText);
        Assert.AreEqual(target.CanonicalText, modal.ExistingTargetText);
        Assert.AreEqual(candidate.CanonicalText, modal.KeepBothCandidateText);
        Assert.AreEqual(1, modal.ConflictCount);
        Assert.AreSame(KeyboardContext.Modal, presentation.InputContext);
        Assert.AreSame(OperationStatus.CopyAwaitingConflict, presentation.OperationStatus);
        Assert.AreEqual(string.Empty, presentation.DeletionConfirmationAutomationId);
        Assert.AreSame(OperationBarTone.AwaitingConfirmation, presentation.Tone);
        AssertConflictInputs(
            (ConflictSet)TransferPreflightOutcome.Conflicted([TransferConflict.Create(source, target, candidate)]),
            modal.Continuation);
    }

    private static void AssertConflictInputs(ConflictSet conflicts, TransferContinuation continuation)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ActiveConflictModal(
            null!, TransferConflictDecision.Cancel, continuation));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ActiveConflictModal(
            conflicts, null!, continuation));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ActiveConflictModal(
            conflicts, TransferConflictDecision.Cancel, null!));
    }

    private static void EnqueueConflictRace(
        QueuedFileOperationPort port,
        TransferConflict firstConflict,
        FileSystemPath racedCandidate)
    {
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(firstConflict.Source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([firstConflict]));
        port.EnqueuePreflight((sources, _) => TransferPreflightOutcome.Conflicted([
            TransferConflict.Create(sources[0], firstConflict.ExistingTarget, racedCandidate)]));
    }
}
