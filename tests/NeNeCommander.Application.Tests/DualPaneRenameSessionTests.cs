using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves DualPaneRenameSession behavior and boundaries.</summary>
[TestClass]
public sealed class DualPaneRenameSessionTests
{
    /// <summary>Proves F2 freezes the focus item into the shared name entry with its current name and frozen panes.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenRenameIsRequestedFreezesFocusItemAndAwaitsName()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));

        DualPaneSnapshot awaiting = await fixture.Panes.HandleAsync(UserIntent.Rename, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot moved = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot activated = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot navigated = await fixture.Panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath("C:\\elsewhere"), CancellationToken.None);
        DualPaneSnapshot escaped = await fixture.Panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationAwaitingName pending = Assert.IsInstanceOfType<OperationAwaitingName>(awaiting.Operation);
        Assert.AreSame(OperationKind.Rename, pending.Kind);
        Assert.AreSame(leftListing.Entries[0].Path, pending.Subject);
        Assert.AreEqual("a.txt", pending.InitialName);
        Assert.AreEqual(awaiting, moved);
        Assert.AreEqual(awaiting, activated);
        Assert.AreEqual(awaiting, navigated);
        Assert.AreSame(OperationActivity.Idle, escaped.Operation);
        Assert.AreSame(PaneSide.Left, escaped.ActiveSide);
        Assert.HasCount(1, fixture.Right.Requests);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves a submitted name renames the frozen focus item and leaves the focus on its new path.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRenameNameIsSubmittedRenamesAndFocusesNewPath()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.Rename, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueueRename(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\left", ("b.txt", DirectoryEntryKind.File)), null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.SubmitName("b.txt"), observer, CancellationToken.None);

        Assert.HasCount(2, fixture.Port.Calls);
        Assert.AreEqual("Inspect:C:\\left\\a.txt", fixture.Port.Calls[0]);
        Assert.AreEqual("Rename:C:\\left\\a.txt>C:\\left\\b.txt", fixture.Port.Calls[1]);
        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation);
        Assert.AreSame(OperationKind.Rename, completed.Kind);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Outcome.Completion);
        Assert.AreEqual("C:\\left\\b.txt", DualPaneSessionTestContext.Focus(snapshot.Left)?.CanonicalText);
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
        Assert.HasCount(1, observer.Snapshots);
        Assert.AreEqual(
            FileOperationProgress.Create(1, 1),
            Assert.IsInstanceOfType<OperationRunning>(observer.Snapshots[0].Operation).Progress);
    }

    /// <summary>Proves a name that only changes letter case still starts the rename through the gateway.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRenameChangesOnlyCaseStartsTheRename()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.Rename, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueueRename(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\left", ("A.TXT", DirectoryEntryKind.File)), null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.SubmitName("A.TXT"), RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreEqual("Rename:C:\\left\\a.txt>C:\\left\\A.TXT", fixture.Port.Calls[1]);
        Assert.AreSame(
            FileOperationCompletionKind.Succeeded,
            Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation).Outcome.Completion);
        Assert.AreEqual("C:\\left\\A.TXT", DualPaneSessionTestContext.Focus(snapshot.Left)?.CanonicalText);
    }

    /// <summary>Proves an invalid new name never reaches the gateway and a provider conflict leaves the focus alone.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public async Task HandleAsyncWhenRenameNameIsInvalidOrRefusedNothingMoves()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.Rename, RecordingDualPaneObserver.Create(), CancellationToken.None);

        DualPaneSnapshot invalid = await fixture.Panes.HandleAsync(UserIntent.SubmitName("bad:name"), RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationRequestRejected rejected = Assert.IsInstanceOfType<OperationRequestRejected>(invalid.Operation);
        Assert.AreSame(OperationKind.Rename, rejected.Kind);
        Assert.AreSame(FileOperationRequestFailureKind.InvalidName, rejected.Failure);
        Assert.IsEmpty(fixture.Port.Calls);

        _ = await fixture.Panes.HandleAsync(UserIntent.Rename, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueueRename(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));

        DualPaneSnapshot refused = await fixture.Panes.HandleAsync(UserIntent.SubmitName("taken.txt"), RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(refused.Operation);
        Assert.AreSame(FileOperationFailureKind.Conflict, completed.Outcome.Failure);
        Assert.AreSame(leftListing.Entries[0].Path, DualPaneSessionTestContext.Focus(refused.Left));
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves F2 without a listing and F2 on an empty listing both leave the session idle.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRenameHasNoFocusItemDoesNothing()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();

        DualPaneSnapshot unlisted = await fixture.Panes.HandleAsync(UserIntent.Rename, RecordingDualPaneObserver.Create(), CancellationToken.None);
        await fixture.ListBothAsync(DualPaneSessionTestContext.Listing("C:\\left"), DualPaneSessionTestContext.Listing("C:\\right"));
        DualPaneSnapshot empty = await fixture.Panes.HandleAsync(UserIntent.Rename, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(OperationActivity.Idle, unlisted.Operation);
        Assert.AreSame(OperationActivity.Idle, empty.Operation);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves the focused entry of a listing whose focus is not its first entry is the one that is frozen.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRenameFollowsMovementFreezesTheMovedFocusItem()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);

        DualPaneSnapshot awaiting = await fixture.Panes.HandleAsync(UserIntent.Rename, RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationAwaitingName pending = Assert.IsInstanceOfType<OperationAwaitingName>(awaiting.Operation);
        Assert.AreSame(leftListing.Entries[1].Path, pending.Subject);
        Assert.AreEqual("b.txt", pending.InitialName);
    }
}
