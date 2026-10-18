using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves DualPaneCreateDirectory behavior and boundaries.</summary>
[TestClass]
public sealed class DualPaneCreateDirectoryTests
{
    /// <summary>Proves F7 freezes the active location into a name-entry state that only escape or a name leaves.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenCreateDirectoryIsRequestedAwaitsNameAndFreezesPanes()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));

        DualPaneSnapshot awaiting = await fixture.Panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot moved = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot hiddenToggle = await fixture.Panes.HandleAsync(UserIntent.ToggleHiddenItems, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot activated = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot navigated = await fixture.Panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath("C:\\elsewhere"), CancellationToken.None);
        DualPaneSnapshot escaped = await fixture.Panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationAwaitingName pending = Assert.IsInstanceOfType<OperationAwaitingName>(awaiting.Operation);
        Assert.AreSame(OperationKind.CreateDirectory, pending.Kind);
        Assert.AreSame(leftListing.Location, pending.Subject);
        Assert.AreEqual(string.Empty, pending.InitialName);
        Assert.AreEqual(awaiting, moved);
        Assert.AreEqual(awaiting, hiddenToggle);
        Assert.AreEqual(awaiting, activated);
        Assert.AreEqual(awaiting, navigated);
        Assert.AreSame(OperationActivity.Idle, escaped.Operation);
        Assert.AreSame(PaneSide.Left, escaped.ActiveSide);
        Assert.AreSame(leftListing.Entries[0].Path, DualPaneSessionTestContext.Focus(escaped.Left));
        Assert.HasCount(1, fixture.Right.Requests);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves a submitted name creates the directory in the frozen location and focuses it after both panes refresh.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenNameIsSubmittedCreatesDirectoryAndFocusesIt()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Location));
        fixture.Port.EnqueueDirectoryCreation(ProviderStepOutcome.Succeeded());
        DirectoryListing leftAfter = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("new", DirectoryEntryKind.Directory));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftAfter, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.SubmitName("new"), observer, CancellationToken.None);

        Assert.HasCount(2, fixture.Port.Calls);
        Assert.AreEqual("Inspect:C:\\left", fixture.Port.Calls[0]);
        Assert.AreEqual("CreateDirectory:C:\\left\\new", fixture.Port.Calls[1]);
        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation);
        Assert.AreSame(OperationKind.CreateDirectory, completed.Kind);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Outcome.Completion);
        Assert.AreEqual("C:\\left\\new", DualPaneSessionTestContext.Focus(snapshot.Left)?.CanonicalText);
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
        Assert.HasCount(1, observer.Snapshots);
        Assert.AreEqual(
            FileOperationProgress.Create(1, 1),
            Assert.IsInstanceOfType<OperationRunning>(observer.Snapshots[0].Operation).Progress);
    }

    /// <summary>Proves an invalid name is rejected before the gateway and a provider rejection refreshes without moving focus.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public async Task HandleAsyncWhenNameIsInvalidOrCreationFailsNothingIsFocused()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None);

        DualPaneSnapshot invalid = await fixture.Panes.HandleAsync(UserIntent.SubmitName("bad:name"), RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationRequestRejected rejected = Assert.IsInstanceOfType<OperationRequestRejected>(invalid.Operation);
        Assert.AreSame(OperationKind.CreateDirectory, rejected.Kind);
        Assert.AreSame(FileOperationRequestFailureKind.InvalidName, rejected.Failure);
        Assert.IsEmpty(fixture.Port.Calls);

        _ = await fixture.Panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Location));
        fixture.Port.EnqueueDirectoryCreation(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));

        DualPaneSnapshot refused = await fixture.Panes.HandleAsync(UserIntent.SubmitName("a.txt"), RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(refused.Operation);
        Assert.AreSame(FileOperationFailureKind.Conflict, completed.Outcome.Failure);
        Assert.AreSame(leftListing.Entries[0].Path, DualPaneSessionTestContext.Focus(refused.Left));
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves F7 without a listing does nothing and a focused refresh before the first listing is a no-op.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCreateDirectoryHasNoListingDoesNothing()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None);
        PaneSnapshot refreshed = await fixture.LeftSession.RefreshFocusingAsync(DualPaneSessionTestContext.ParsePath("C:\\left\\new"), CancellationToken.None);

        Assert.AreSame(OperationActivity.Idle, snapshot.Operation);
        Assert.AreEqual(PaneSnapshot.Initial, refreshed);
        Assert.IsEmpty(fixture.Left.Requests);
    }
}
