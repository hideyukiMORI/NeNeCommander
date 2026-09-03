using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the dual-pane coordinator routes intents to the sole active pane and runs moves through the gateway.</summary>
[TestClass]
public sealed class DualPaneSessionTests
{
    /// <summary>Proves the left pane is active before any intent and both panes start empty.</summary>
    [TestMethod]
    public void CurrentWhenNothingHappenedReportsLeftActiveAndBothInitial()
    {
        using Fixture fixture = Fixture.Create();

        DualPaneSnapshot snapshot = fixture.Panes.Current;

        Assert.AreSame(PaneSide.Left, snapshot.ActiveSide);
        Assert.AreSame(PaneSnapshot.Initial, snapshot.Left);
        Assert.AreSame(PaneSnapshot.Initial, snapshot.Right);
        Assert.AreSame(OperationActivity.Idle, snapshot.Operation);
    }

    /// <summary>Proves each side reads its own location regardless of the active side.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenSideIsGivenReadsIntoThatSideOnly()
    {
        using Fixture fixture = Fixture.Create();
        DirectoryListing rightListing = Listing("C:\\Users", ("xi", DirectoryEntryKind.Directory));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));

        DualPaneSnapshot snapshot = await fixture.Panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\Users"), CancellationToken.None);

        Assert.IsEmpty(fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
        Assert.AreSame(rightListing, Assert.IsInstanceOfType<PaneContentListed>(snapshot.Right.Content).Listing);
        Assert.AreSame(PaneContent.Absent, snapshot.Left.Content);
        Assert.AreSame(PaneSide.Left, snapshot.ActiveSide);
        Assert.AreSame(snapshot.Right, snapshot.Of(PaneSide.Right));
        Assert.AreSame(snapshot.Left, snapshot.Of(PaneSide.Left));
        Assert.AreNotSame(snapshot.Left, snapshot.Right);
    }

    /// <summary>Proves activation toggles the side without touching either pane's focus.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenActivateOtherPaneTogglesActiveSideWithoutMovingFocus()
    {
        using Fixture fixture = Fixture.Create();
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File))));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", ("c.txt", DirectoryEntryKind.File), ("d.txt", DirectoryEntryKind.File))));
        _ = await fixture.Panes.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        DualPaneSnapshot before = await fixture.Panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);

        DualPaneSnapshot toRight = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot backToLeft = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(PaneSide.Right, toRight.ActiveSide);
        Assert.AreSame(PaneSide.Left, backToLeft.ActiveSide);
        Assert.AreSame(before.Left, toRight.Left);
        Assert.AreSame(before.Right, toRight.Right);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Proves movement reaches only the active pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMovementArrivesOnlyActivePaneChanges()
    {
        using Fixture fixture = Fixture.Create();
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        DirectoryListing rightListing = Listing("C:\\right", ("c.txt", DirectoryEntryKind.File), ("d.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, rightListing);

        DualPaneSnapshot leftMoved = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot rightMoved = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(leftListing.Entries[1].Path, Focus(leftMoved.Left));
        Assert.AreSame(rightListing.Entries[0].Path, Focus(leftMoved.Right));
        Assert.AreSame(leftListing.Entries[1].Path, Focus(rightMoved.Left));
        Assert.AreSame(rightListing.Entries[1].Path, Focus(rightMoved.Right));
    }

    /// <summary>Proves a read in flight lands in the pane that started it even after activation changes.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenActivationChangesDuringReadResultLandsInOriginPane()
    {
        using Fixture fixture = Fixture.Create();
        DirectoryListing rightListing = Listing("C:\\right", ("c.txt", DirectoryEntryKind.File), ("d.txt", DirectoryEntryKind.File));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        TaskCompletionSource<DirectoryReadOutcome> pendingLeft = fixture.Left.EnqueuePending();
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        _ = await fixture.Panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);

        Task<DualPaneSnapshot> leftNavigation = fixture.Panes.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        DualPaneSnapshot switched = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot rightMoved = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        pendingLeft.SetResult(DirectoryReadOutcome.Succeeded(leftListing));
        DualPaneSnapshot completed = await leftNavigation;

        _ = Assert.IsInstanceOfType<PaneLoading>(switched.Left.Activity);
        Assert.AreSame(PaneSide.Right, switched.ActiveSide);
        Assert.AreSame(rightListing.Entries[1].Path, Focus(rightMoved.Right));
        Assert.AreSame(leftListing, Assert.IsInstanceOfType<PaneContentListed>(completed.Left.Content).Listing);
        Assert.AreSame(rightListing, Assert.IsInstanceOfType<PaneContentListed>(completed.Right.Content).Listing);
        Assert.AreSame(PaneSide.Right, completed.ActiveSide);
        Assert.HasCount(1, fixture.Left.Requests);
    }

    /// <summary>Proves the focus item moves to the passive location and both panes are re-read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMoveHasNoSelectionMovesFocusItemAndRefreshesBothPanes()
    {
        using Fixture fixture = Fixture.Create();
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, Listing("C:\\right"));
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        DirectoryListing leftAfter = Listing("C:\\left", ("b.txt", DirectoryEntryKind.File));
        DirectoryListing rightAfter = Listing("C:\\right", ("a.txt", DirectoryEntryKind.File));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftAfter));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightAfter));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreEqual("Preflight:C:\\right", fixture.Port.Calls[1]);
        Assert.AreEqual("Copy:C:\\left\\a.txt", fixture.Port.Calls[2]);
        Assert.AreSame(
            FileOperationCompletionKind.Succeeded,
            Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation).Outcome.Completion);
        Assert.AreSame(leftAfter, Assert.IsInstanceOfType<PaneContentListed>(snapshot.Left.Content).Listing);
        Assert.AreSame(rightAfter, Assert.IsInstanceOfType<PaneContentListed>(snapshot.Right.Content).Listing);
        Assert.AreSame(leftAfter.Entries[0].Path, Focus(snapshot.Left));
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves a copy takes the focus item to the passive location, deletes nothing, and refreshes both panes.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCopyHasNoSelectionCopiesFocusItemWithoutDeletingSource()
    {
        using Fixture fixture = Fixture.Create();
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, Listing("C:\\right"));
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        DirectoryListing rightAfter = Listing("C:\\right", ("a.txt", DirectoryEntryKind.File));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightAfter));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.HasCount(4, fixture.Port.Calls);
        Assert.AreEqual("Preflight:C:\\right", fixture.Port.Calls[1]);
        Assert.AreEqual("Copy:C:\\left\\a.txt", fixture.Port.Calls[2]);
        Assert.AreEqual("Verify:C:\\left\\a.txt", fixture.Port.Calls[3]);
        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation);
        Assert.AreSame(OperationKind.Copy, completed.Kind);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Outcome.Completion);
        Assert.AreSame(rightAfter, Assert.IsInstanceOfType<PaneContentListed>(snapshot.Right.Content).Listing);
        Assert.AreSame(leftListing.Entries[0].Path, Focus(snapshot.Left));
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves a copy whose destination is one of its sources is rejected before the gateway.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCopyTargetsItsOwnSourceRecordsRequestRejection()
    {
        using Fixture fixture = Fixture.Create();
        await fixture.ListBothAsync(Listing("C:\\", ("Users", DirectoryEntryKind.Directory)), Listing("C:\\Users"));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationRequestRejected rejected = Assert.IsInstanceOfType<OperationRequestRejected>(snapshot.Operation);
        Assert.AreSame(FileOperationRequestFailureKind.DestinationIsSource, rejected.Failure);
        Assert.AreSame(OperationKind.Copy, rejected.Kind);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves escape during a running operation cancels it at the gateway's next observation point and the next operation starts fresh.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task HandleAsyncWhenEscapeArrivesDuringOperationCancelsAtNextObservationPoint()
    {
        Fixture? running = null;
        using Fixture fixture = Fixture.Create(
            ScriptedCallbackPoint.AfterInspection,
            () => _ = running?.Panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None));
        running = fixture;
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, Listing("C:\\right"));
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right")));

        DualPaneSnapshot cancelled = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(cancelled.Operation);
        Assert.AreSame(OperationKind.Copy, completed.Kind);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, completed.Outcome.Completion);
        Assert.IsEmpty(completed.Outcome.Effects);
        Assert.HasCount(2, fixture.Port.Calls);
        Assert.AreEqual("Preflight:C:\\right", fixture.Port.Calls[1]);
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);

        running = null;
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", ("a.txt", DirectoryEntryKind.File))));

        DualPaneSnapshot second = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(
            FileOperationCompletionKind.Succeeded,
            Assert.IsInstanceOfType<OperationCompleted>(second.Operation).Outcome.Completion);
        Assert.HasCount(6, fixture.Port.Calls);
    }

    /// <summary>Proves a non-escape intent during a running operation neither cancels it nor changes the snapshot.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task HandleAsyncWhenOtherIntentArrivesDuringOperationDoesNotCancelIt()
    {
        Fixture? running = null;
        using Fixture fixture = Fixture.Create(
            ScriptedCallbackPoint.AfterInspection,
            () => _ = running?.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None));
        running = fixture;
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, Listing("C:\\right"));
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", ("a.txt", DirectoryEntryKind.File))));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(
            FileOperationCompletionKind.Succeeded,
            Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation).Outcome.Completion);
        Assert.HasCount(4, fixture.Port.Calls);
        Assert.AreSame(leftListing.Entries[0].Path, Focus(snapshot.Left));
    }

    /// <summary>Proves the running activity starts at zero progress and the observer sees each completed source before the intent completes.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSourcesCompleteObserverSeesRunningProgress()
    {
        Fixture? running = null;
        OperationActivity? atInspection = null;
        using Fixture fixture = Fixture.Create(
            ScriptedCallbackPoint.AfterInspection,
            () => atInspection ??= running?.Panes.Current.Operation);
        running = fixture;
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, Listing("C:\\right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        foreach (DirectoryEntry entry in leftListing.Entries)
        {
            fixture.Port.EnqueueInspection(Inspection(entry.Path));
        }
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        for (int index = 0; index < 2; index++)
        {
            fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
            fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        }
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right")));
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Copy, observer, CancellationToken.None);

        OperationRunning initial = Assert.IsInstanceOfType<OperationRunning>(atInspection);
        Assert.AreSame(OperationKind.Copy, initial.Kind);
        Assert.AreEqual(FileOperationProgress.Create(0, 2), initial.Progress);
        Assert.HasCount(2, observer.Snapshots);
        Assert.AreEqual(
            FileOperationProgress.Create(1, 2),
            Assert.IsInstanceOfType<OperationRunning>(observer.Snapshots[0].Operation).Progress);
        Assert.AreEqual(
            FileOperationProgress.Create(2, 2),
            Assert.IsInstanceOfType<OperationRunning>(observer.Snapshots[1].Operation).Progress);
        Assert.AreSame(PaneSide.Left, observer.Snapshots[1].ActiveSide);
        _ = Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation);
    }

    /// <summary>Proves an explicit selection is moved instead of the focus item and focus is kept on refresh.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMoveHasSelectionMovesSelectionAndKeepsFocus()
    {
        using Fixture fixture = Fixture.Create();
        DirectoryListing leftListing = Listing(
            "C:\\left",
            ("a.txt", DirectoryEntryKind.File),
            ("b.txt", DirectoryEntryKind.File),
            ("c.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, Listing("C:\\right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[1].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        for (int index = 0; index < 2; index++)
        {
            fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
            fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
            fixture.Port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        }
        DirectoryListing leftAfter = Listing("C:\\left", ("c.txt", DirectoryEntryKind.File));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftAfter));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File))));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreEqual("Inspect:C:\\left\\a.txt", fixture.Port.Calls[0]);
        Assert.AreEqual("Inspect:C:\\left\\b.txt", fixture.Port.Calls[1]);
        Assert.HasCount(9, fixture.Port.Calls);
        PaneContentListed left = Assert.IsInstanceOfType<PaneContentListed>(snapshot.Left.Content);
        Assert.AreSame(leftAfter.Entries[0].Path, left.State.FocusItem);
        Assert.IsEmpty(left.State.Selection);
    }

    /// <summary>Proves a move without a listed passive pane or without a focus item starts nothing.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMoveHasNoDestinationOrNoSourceDoesNothing()
    {
        using Fixture fixture = Fixture.Create();
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", ("a.txt", DirectoryEntryKind.File))));
        DualPaneSnapshot leftOnly = await fixture.Panes.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);

        DualPaneSnapshot noDestination = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right")));
        _ = await fixture.Panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot noSource = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreEqual(leftOnly, noDestination);
        Assert.AreSame(OperationActivity.Idle, noSource.Operation);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves a request the gateway would reject is recorded without touching the port.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMoveTargetsItsOwnSourceRecordsRequestRejection()
    {
        using Fixture fixture = Fixture.Create();
        await fixture.ListBothAsync(Listing("C:\\", ("Users", DirectoryEntryKind.Directory)), Listing("C:\\Users"));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(
            FileOperationRequestFailureKind.DestinationIsSource,
            Assert.IsInstanceOfType<OperationRequestRejected>(snapshot.Operation).Failure);
        Assert.AreSame(OperationKind.Move, Assert.IsInstanceOfType<OperationRequestRejected>(snapshot.Operation).Kind);
        Assert.IsEmpty(fixture.Port.Calls);
        Assert.HasCount(1, fixture.Left.Requests);
    }

    /// <summary>Proves a failed gateway outcome is recorded and both panes are still re-read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMoveFailsRecordsOutcomeAndRefreshesPanes()
    {
        using Fixture fixture = Fixture.Create();
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, Listing("C:\\right"));
        fixture.Port.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.AccessDenied));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right")));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation);
        Assert.AreSame(OperationKind.Move, completed.Kind);
        Assert.AreSame(FileOperationCompletionKind.Rejected, completed.Outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, completed.Outcome.Failure);
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves every intent and navigation is frozen while a move runs.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenMoveIsRunningFreezesIntentsAndNavigation()
    {
        using Fixture fixture = Fixture.Create();
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, Listing("C:\\right"));
        BlockingInspectionPort blocking = BlockingInspectionPort.Create(Inspection(leftListing.Entries[0].Path));
        using FileOperationGateway blockingGateway = new(blocking);
        DualPaneSession panes = new(fixture.LeftSession, fixture.RightSession, blockingGateway);

        Task<DualPaneSnapshot> move = panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenIntent = await panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenActivation = await panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenNavigation = await panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\other"), CancellationToken.None);
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", ("a.txt", DirectoryEntryKind.File))));
        blocking.Release();
        DualPaneSnapshot completed = await move;

        Assert.AreSame(OperationKind.Move, Assert.IsInstanceOfType<OperationRunning>(frozenIntent.Operation).Kind);
        Assert.AreSame(leftListing.Entries[0].Path, Focus(frozenIntent.Left));
        Assert.AreSame(PaneSide.Left, frozenActivation.ActiveSide);
        Assert.AreEqual("C:\\right", Assert.IsInstanceOfType<PaneContentListed>(frozenNavigation.Right.Content).Listing.Location.CanonicalText);
        _ = Assert.IsInstanceOfType<OperationCompleted>(completed.Operation);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves an unconfirmed permanent deletion never deletes and waits for confirmation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task HandleAsyncWhenDeleteNeedsConfirmationWaitsWithoutDeleting()
    {
        using Fixture fixture = Fixture.Create();
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, Listing("C:\\right"));
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[0].Path, DeletionCapability.PermanentOnly));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationAwaitingConfirmation awaiting = Assert.IsInstanceOfType<OperationAwaitingConfirmation>(snapshot.Operation);
        Assert.HasCount(1, awaiting.Request.Sources);
        Assert.AreSame(leftListing.Entries[0].Path, awaiting.Request.Sources[0]);
        Assert.HasCount(1, fixture.Port.Calls);
        Assert.HasCount(1, fixture.Left.Requests);
    }

    /// <summary>Proves confirm executes the exact frozen set and escape abandons it, while other intents are frozen.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenConfirmationIsPendingOnlyConfirmOrEscapeResolveIt()
    {
        using Fixture fixture = Fixture.Create();
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, Listing("C:\\right"));
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[0].Path, DeletionCapability.PermanentOnly));
        _ = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);

        DualPaneSnapshot frozenMove = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenActivation = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenNavigation = await fixture.Panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\other"), CancellationToken.None);
        DualPaneSnapshot escaped = await fixture.Panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[0].Path, DeletionCapability.PermanentOnly));
        _ = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[0].Path, DeletionCapability.PermanentOnly));
        fixture.Port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        DirectoryListing leftAfter = Listing("C:\\left", ("b.txt", DirectoryEntryKind.File));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftAfter));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right")));
        DualPaneSnapshot confirmed = await fixture.Panes.HandleAsync(UserIntent.Confirm, RecordingDualPaneObserver.Create(), CancellationToken.None);

        _ = Assert.IsInstanceOfType<OperationAwaitingConfirmation>(frozenMove.Operation);
        Assert.AreSame(leftListing.Entries[0].Path, Focus(frozenMove.Left));
        Assert.AreSame(PaneSide.Left, frozenActivation.ActiveSide);
        Assert.AreEqual("C:\\right", Assert.IsInstanceOfType<PaneContentListed>(frozenNavigation.Right.Content).Listing.Location.CanonicalText);
        Assert.AreSame(OperationActivity.Idle, escaped.Operation);
        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(confirmed.Operation);
        Assert.AreSame(OperationKind.Delete, completed.Kind);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Outcome.Completion);
        Assert.AreEqual("Delete:C:\\left\\a.txt", fixture.Port.Calls[3]);
        Assert.AreSame(leftAfter, Assert.IsInstanceOfType<PaneContentListed>(confirmed.Left.Content).Listing);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves a provider that recycles deletes without confirmation and a delete without a focus item does nothing.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenProviderRecyclesDeletesWithoutConfirmation()
    {
        using Fixture fixture = Fixture.Create();
        DirectoryListing leftListing = Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, Listing("C:\\right"));
        fixture.Port.EnqueueInspection(Inspection(leftListing.Entries[0].Path, DeletionCapability.Recycle));
        fixture.Port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left")));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right")));

        DualPaneSnapshot recycled = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot nothingFocused = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(recycled.Operation);
        Assert.AreSame(FileOperationEffectKind.Recycled, completed.Outcome.Effects[0].Kind);
        Assert.AreEqual(recycled, nothingFocused);
        Assert.HasCount(2, fixture.Port.Calls);
    }

    /// <summary>Proves delete without a listed active pane does nothing.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDeleteHasNoListingDoesNothing()
    {
        using Fixture fixture = Fixture.Create();

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(OperationActivity.Idle, snapshot.Operation);
        Assert.IsEmpty(fixture.Port.Calls);
    }
    /// <summary>Proves one session cannot serve both sides.</summary>
    [TestMethod]
    public void ConstructWhenBothSidesShareOneSessionThrowsArgumentException()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        PaneSession shared = new(port, Capacity(4), DirectoryListing.EntryBoundaryLimit);
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));

        ArgumentException failure = Assert.ThrowsExactly<ArgumentException>(() => new DualPaneSession(shared, shared, gateway));

        Assert.AreEqual("right", failure.ParamName);
        Assert.StartsWith("Each pane side requires its own session.", failure.Message);
    }

    private static FileInspectionOutcome Inspection(FileSystemPath path)
    {
        return Inspection(path, DeletionCapability.Recycle);
    }

    private static FileInspectionOutcome Inspection(FileSystemPath path, DeletionCapability capability)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(path, identity.Identity, capability));
    }

    private static FileSystemPath? Focus(PaneSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content).State.FocusItem;
    }

    private static VisiblePageCapacity Capacity(int rows)
    {
        return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(rows)).Capacity;
    }

    private static DirectoryListing Listing(string location, params (string Name, DirectoryEntryKind Kind)[] entries)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] built = new DirectoryEntry[entries.Length];
        for (int index = 0; index < entries.Length; index++)
        {
            string separator = parsedLocation.CanonicalText.EndsWith('\\') ? string.Empty : "\\";
            built[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + separator + entries[index].Name),
                entries[index].Name,
                entries[index].Kind);
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            built,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }

    private sealed class Fixture : IDisposable
    {
        private Fixture(
            ScriptedDirectoryReadPort left,
            ScriptedDirectoryReadPort right,
            ScriptedFileOperationPort port,
            FileOperationGateway gateway)
        {
            Left = left;
            Right = right;
            Port = port;
            Gateway = gateway;
            LeftSession = new PaneSession(left, Capacity(4), DirectoryListing.EntryBoundaryLimit);
            RightSession = new PaneSession(right, Capacity(4), DirectoryListing.EntryBoundaryLimit);
            Panes = new DualPaneSession(LeftSession, RightSession, gateway);
        }

        internal ScriptedDirectoryReadPort Left { get; }

        internal ScriptedDirectoryReadPort Right { get; }

        internal ScriptedFileOperationPort Port { get; }

        internal FileOperationGateway Gateway { get; }

        internal PaneSession LeftSession { get; }

        internal PaneSession RightSession { get; }

        internal DualPaneSession Panes { get; }

        internal static Fixture Create()
        {
            return Create(null, null);
        }

        internal static Fixture Create(ScriptedCallbackPoint? callbackPoint, Action? callback)
        {
            ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(callbackPoint, callback);
            return new Fixture(
                ScriptedDirectoryReadPort.Create(),
                ScriptedDirectoryReadPort.Create(),
                port,
                new FileOperationGateway(port));
        }

        internal async Task ListBothAsync(DirectoryListing leftListing, DirectoryListing rightListing)
        {
            Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
            Right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
            _ = await Panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
            _ = await Panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        }

        public void Dispose()
        {
            Gateway.Dispose();
        }
    }
}
