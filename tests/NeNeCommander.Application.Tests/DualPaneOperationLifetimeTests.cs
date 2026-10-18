using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves DualPaneOperationLifetime behavior and boundaries.</summary>
[TestClass]
public sealed class DualPaneOperationLifetimeTests
{
    /// <summary>Proves escape during a running operation cancels it at the gateway's next observation point and the next operation starts fresh.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task HandleAsyncWhenEscapeArrivesDuringOperationCancelsAtNextObservationPoint()
    {
        DualPaneSessionTestContext? running = null;
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create(
            ScriptedCallbackPoint.AfterInspection,
            () => _ = running?.Panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None));
        running = fixture;
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));

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
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File)), null));

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
        DualPaneSessionTestContext? running = null;
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create(
            ScriptedCallbackPoint.AfterInspection,
            () => _ = running?.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None));
        running = fixture;
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File)), null));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(
            FileOperationCompletionKind.Succeeded,
            Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation).Outcome.Completion);
        Assert.HasCount(4, fixture.Port.Calls);
        Assert.AreSame(leftListing.Entries[0].Path, DualPaneSessionTestContext.Focus(snapshot.Left));
    }

    /// <summary>Proves the running activity starts at zero progress and the observer sees each completed source before the intent completes.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSourcesCompleteObserverSeesRunningProgress()
    {
        DualPaneSessionTestContext? running = null;
        OperationActivity? atInspection = null;
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create(
            ScriptedCallbackPoint.AfterInspection,
            () => atInspection ??= running?.Panes.Current.Operation);
        running = fixture;
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        foreach (DirectoryEntry entry in leftListing.Entries)
        {
            fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(entry.Path));
        }
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        for (int index = 0; index < 2; index++)
        {
            fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
            fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        }
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));
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

    /// <summary>Proves a defect cannot leave a running state or a disposed cancellation delegate behind.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenGatewayFaultsReleasesRunningStateAndCancellationOwnership()
    {
        InvalidOperationException defect = new("Injected gateway defect.");
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create(
            ScriptedCallbackPoint.AfterInspection,
            () => throw defect);
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));

        InvalidOperationException observed = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await fixture.Panes.HandleAsync(
                UserIntent.Move,
                RecordingDualPaneObserver.Create(),
                CancellationToken.None));
        DualPaneSnapshot afterEscape = await fixture.Panes.HandleAsync(
            UserIntent.Escape,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);

        Assert.AreSame(defect, observed);
        Assert.AreSame(OperationActivity.Idle, afterEscape.Operation);
    }

    /// <summary>Proves every intent and navigation is frozen while a move runs.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenMoveIsRunningFreezesIntentsAndNavigation()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        BlockingInspectionPort blocking = BlockingInspectionPort.Create(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        using FileOperationGateway blockingGateway = new(blocking);
        DualPaneSession panes = new(fixture.LeftSession, fixture.RightSession, blockingGateway);

        Task<DualPaneSnapshot> move = panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenIntent = await panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenHiddenToggle = await panes.HandleAsync(UserIntent.ToggleHiddenItems, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenActivation = await panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenNavigation = await panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath("C:\\other"), CancellationToken.None);
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File)), null));
        blocking.Release();
        DualPaneSnapshot completed = await move;

        Assert.AreSame(OperationKind.Move, Assert.IsInstanceOfType<OperationRunning>(frozenIntent.Operation).Kind);
        Assert.AreSame(leftListing.Entries[0].Path, DualPaneSessionTestContext.Focus(frozenIntent.Left));
        Assert.AreSame(frozenIntent.Left, frozenHiddenToggle.Left);
        Assert.AreSame(frozenIntent.Right, frozenHiddenToggle.Right);
        Assert.AreSame(PaneSide.Left, frozenActivation.ActiveSide);
        Assert.AreEqual("C:\\right", Assert.IsInstanceOfType<PaneContentListed>(frozenNavigation.Right.Content).Listing.Location.CanonicalText);
        _ = Assert.IsInstanceOfType<OperationCompleted>(completed.Operation);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves a running operation takes Escape before the active pane's read, which keeps loading.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task HandleAsyncWhenOperationRunsWhileActivePaneLoadsEscapeCancelsOperationFirst()
    {
        DualPaneSessionTestContext? running = null;
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create(
            ScriptedCallbackPoint.AfterInspection,
            () => _ = running?.Panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None));
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        TaskCompletionSource<DirectoryReadOutcome> leftRead = fixture.Left.EnqueuePending();
        Task<DualPaneSnapshot> leftNavigation = fixture.Panes.NavigateAsync(PaneSide.Left, DualPaneSessionTestContext.ParsePath("C:\\left-target"), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));
        running = fixture;

        DualPaneSnapshot cancelled = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(
            FileOperationCompletionKind.Cancelled,
            Assert.IsInstanceOfType<OperationCompleted>(cancelled.Operation).Outcome.Completion);
        _ = Assert.IsInstanceOfType<PaneLoading>(cancelled.Left.Activity);
        Assert.IsFalse(fixture.Left.Tokens[1].IsCancellationRequested);
        leftRead.SetResult(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\left-target"), null));
        DualPaneSnapshot completed = await leftNavigation;
        Assert.AreEqual("C:\\left-target", Assert.IsInstanceOfType<PaneContentListed>(completed.Left.Content).Listing.Location.CanonicalText);
    }
}
