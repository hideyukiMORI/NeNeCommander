using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves DualPaneDeletionSession behavior and boundaries.</summary>
[TestClass]
public sealed class DualPaneDeletionSessionTests
{
    /// <summary>Proves a sort intent reorders only the active pane and is frozen while a confirmation is pending.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSortIntentArrivesReordersActivePaneUnlessConfirmationIsPending()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.md", DirectoryEntryKind.File));
        DirectoryListing rightListing = DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File), ("b.md", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, rightListing);

        DualPaneSnapshot sorted = await fixture.Panes.HandleAsync(
            UserIntent.SortByExtension,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path, DeletionCapability.PermanentOnly));
        DualPaneSnapshot awaiting = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozen = await fixture.Panes.HandleAsync(UserIntent.SortByName, RecordingDualPaneObserver.Create(), CancellationToken.None);

        string[] expectedLeft = ["b.md", "a.txt"];
        CollectionAssert.AreEqual(
            expectedLeft,
            Assert.IsInstanceOfType<PaneContentListed>(sorted.Left.Content).State.VisibleEntries.Select(entry => entry.Name).ToArray());
        Assert.AreEqual(
            PaneSortOrder.Default,
            Assert.IsInstanceOfType<PaneContentListed>(sorted.Right.Content).State.SortOrder);
        Assert.AreSame(leftListing.Entries[0].Path, DualPaneSessionTestContext.Focus(sorted.Left));
        _ = Assert.IsInstanceOfType<OperationAwaitingConfirmation>(frozen.Operation);
        Assert.AreSame(awaiting.Left, frozen.Left);
        Assert.AreSame(awaiting.Right, frozen.Right);
    }

    /// <summary>Proves an unconfirmed permanent deletion never deletes and waits for confirmation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task HandleAsyncWhenDeleteNeedsConfirmationWaitsWithoutDeleting()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path, DeletionCapability.PermanentOnly));

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
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path, DeletionCapability.PermanentOnly));
        _ = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);

        DualPaneSnapshot frozenMove = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenHiddenToggle = await fixture.Panes.HandleAsync(UserIntent.ToggleHiddenItems, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenActivation = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot frozenNavigation = await fixture.Panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath("C:\\other"), CancellationToken.None);
        DualPaneSnapshot escaped = await fixture.Panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path, DeletionCapability.PermanentOnly));
        _ = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path, DeletionCapability.PermanentOnly));
        fixture.Port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        DirectoryListing leftAfter = DualPaneSessionTestContext.Listing("C:\\left", ("b.txt", DirectoryEntryKind.File));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftAfter, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));
        DualPaneSnapshot confirmed = await fixture.Panes.HandleAsync(UserIntent.Confirm, RecordingDualPaneObserver.Create(), CancellationToken.None);

        _ = Assert.IsInstanceOfType<OperationAwaitingConfirmation>(frozenMove.Operation);
        Assert.AreSame(leftListing.Entries[0].Path, DualPaneSessionTestContext.Focus(frozenMove.Left));
        Assert.AreSame(frozenMove.Left, frozenHiddenToggle.Left);
        Assert.AreSame(frozenMove.Right, frozenHiddenToggle.Right);
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
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path, DeletionCapability.Recycle));
        fixture.Port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\left"), null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));

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
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(OperationActivity.Idle, snapshot.Operation);
        Assert.IsEmpty(fixture.Port.Calls);
    }
}
