using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the current-location command navigates only its captured opposite pane.</summary>
[TestClass]
public sealed class OtherPaneNavigationTests
{
    /// <summary>Proves left-to-right navigation keeps the source snapshot and active side.</summary>
    [TestMethod]
    public Task HandleAsyncWhenLeftIsActiveNavigatesRightWithoutChangingSourceAsync()
    {
        return AssertDirectionAsync(PaneSide.Left);
    }

    /// <summary>Proves right-to-left navigation keeps the source snapshot and active side.</summary>
    [TestMethod]
    public Task HandleAsyncWhenRightIsActiveNavigatesLeftWithoutChangingSourceAsync()
    {
        return AssertDirectionAsync(PaneSide.Right);
    }

    /// <summary>Proves an absent opposite pane is populated through its own provider read.</summary>
    [TestMethod]
    [DataRow(@"C:\source")]
    [DataRow(@"\\nas\share\source")]
    [DataRow(@"\\wsl.localhost\Ubuntu\home\sample")]
    public async Task HandleAsyncWhenOppositeIsAbsentUsesExistingReadForSourceProviderAsync(string path)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing source = DualPaneSessionTestContext.Listing(path, ("focused.txt", DirectoryEntryKind.File));
        DirectoryListing fresh = DualPaneSessionTestContext.Listing(path, ("fresh.txt", DirectoryEntryKind.File));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(source, null));
        DualPaneSnapshot before = await fixture.Panes.NavigateAsync(PaneSide.Left, source.Location, CancellationToken.None);
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(fresh, null));

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(PaneSide.Left, after.ActiveSide);
        Assert.AreSame(fresh, Listed(after.Right).Listing);
        Assert.AreSame(source.Location, fixture.Right.Requests[0].Location);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves equivalent provider-aware locations are exact snapshot-preserving no-ops.</summary>
    [TestMethod]
    [DataRow(@"C:\Source", @"c:\source")]
    [DataRow(@"\\NAS\SHARE\Source", @"\\nas\share\source")]
    [DataRow(@"\\wsl.localhost\Ubuntu\home\Sample", @"\\wsl$\ubuntu\home\Sample")]
    public async Task HandleAsyncWhenLocationsHaveSameIdentityPerformsNoReadAsync(string sourcePath, string targetPath)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing source = DualPaneSessionTestContext.Listing(sourcePath, ("source.txt", DirectoryEntryKind.File));
        DirectoryListing target = DualPaneSessionTestContext.Listing(targetPath, ("target.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(source, target);
        _ = await fixture.RightSession.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        DualPaneSnapshot before = await HandleAsync(fixture, UserIntent.ToggleSelection);

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(before.Right, after.Right);
        Assert.AreSame(before.Operation, after.Operation);
        Assert.AreSame(PaneSide.Left, after.ActiveSide);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves distinct Linux component casing is not treated as an equal WSL location.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenWslComponentCaseDiffersNavigatesOppositeAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing source = DualPaneSessionTestContext.Listing(@"\\wsl.localhost\Ubuntu\home\Sample");
        DirectoryListing target = DualPaneSessionTestContext.Listing(@"\\wsl.localhost\ubuntu\home\sample");
        await fixture.ListBothAsync(source, target);
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(source, null));

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        Assert.HasCount(2, fixture.Right.Requests);
        Assert.AreSame(source.Location, fixture.Right.Requests[1].Location);
        Assert.AreSame(source.Location, Listed(after.Right).State.Location);
        Assert.AreSame(target.Location, Listed(after.Right).State.NavigationHistory.BackTarget);
    }

    /// <summary>Proves the destination preserves its own sorting/visibility and appends its history.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDestinationHasPreferencesKeepsItsOwnOrderAndHistoryAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing source = DualPaneSessionTestContext.Listing(@"C:\source", ("source.txt", DirectoryEntryKind.File));
        DirectoryListing target = DualPaneSessionTestContext.Listing(@"D:\target", ("selected.txt", DirectoryEntryKind.File));
        DirectoryListing fresh = DualPaneSessionTestContext.ListingWithVisibility(@"C:\source",
            ("a.txt", DirectoryEntryKind.File, EntryVisibility.Normal),
            ("z.txt", DirectoryEntryKind.File, EntryVisibility.Hidden));
        await fixture.ListBothAsync(source, target);
        _ = await fixture.RightSession.HandleAsync(UserIntent.SortByName, CancellationToken.None);
        _ = await fixture.RightSession.HandleAsync(UserIntent.ToggleHiddenItems, CancellationToken.None);
        _ = await fixture.RightSession.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        DualPaneSnapshot before = fixture.Panes.Current;
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(fresh, null));

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        PaneState changed = Listed(after.Right).State;
        Assert.AreSame(before.Left, after.Left);
        Assert.AreEqual(Listed(before.Right).State.SortOrder, changed.SortOrder);
        Assert.AreSame(Listed(before.Right).State.HiddenItemVisibility, changed.HiddenItemVisibility);
        Assert.AreSame(fresh.Entries[1].Path, changed.FocusItem);
        Assert.HasCount(2, changed.VisibleEntries);
        Assert.IsEmpty(changed.Selection);
        Assert.AreSame(target.Location, changed.NavigationHistory.BackTarget);
        Assert.HasCount(2, changed.NavigationHistory.Locations);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves failed target navigation retains every destination content value and reports its typed failure.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDestinationReadFailsKeepsItsContentAndSourceAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing source = DualPaneSessionTestContext.Listing(@"C:\source");
        DirectoryListing target = DualPaneSessionTestContext.Listing(@"D:\target", ("selected.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(source, target);
        _ = await fixture.RightSession.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        DualPaneSnapshot before = fixture.Panes.Current;
        fixture.Right.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(before.Right.Content, after.Right.Content);
        PaneReadFailed failed = Assert.IsInstanceOfType<PaneReadFailed>(after.Right.Activity);
        Assert.AreSame(source.Location, failed.Target);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, failed.Failure);
        Assert.HasCount(2, fixture.Right.Requests);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves provider cancellation retains the destination content and source snapshot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDestinationReadIsCancelledKeepsItsContentAndSourceAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing source = DualPaneSessionTestContext.Listing(@"C:\source");
        DirectoryListing target = DualPaneSessionTestContext.Listing(@"D:\target", ("selected.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(source, target);
        _ = await fixture.RightSession.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        DualPaneSnapshot before = fixture.Panes.Current;
        fixture.Right.Enqueue(DirectoryReadOutcome.Cancelled());

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(before.Right.Content, after.Right.Content);
        Assert.AreSame(source.Location, Assert.IsInstanceOfType<PaneReadCancelled>(after.Right.Activity).Target);
        Assert.HasCount(2, fixture.Right.Requests);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    private static async Task AssertDirectionAsync(PaneSide sourceSide)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing left = DualPaneSessionTestContext.Listing(@"C:\left",
            ("first.txt", DirectoryEntryKind.File), ("selected.txt", DirectoryEntryKind.File));
        DirectoryListing right = DualPaneSessionTestContext.Listing(@"D:\right",
            ("first.txt", DirectoryEntryKind.File), ("selected.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(left, right);
        if (sourceSide == PaneSide.Right)
        {
            _ = await HandleAsync(fixture, UserIntent.ActivateOtherPane);
        }
        _ = await HandleAsync(fixture, UserIntent.FocusLast);
        DualPaneSnapshot before = await HandleAsync(fixture, UserIntent.ToggleSelection);
        DirectoryListing fresh = DualPaneSessionTestContext.Listing(Listed(before.Of(sourceSide)).State.Location.CanonicalText,
            ("fresh.txt", DirectoryEntryKind.File));
        ScriptedDirectoryReadPort destination = sourceSide == PaneSide.Left ? fixture.Right : fixture.Left;
        destination.Enqueue(DirectoryReadOutcome.Succeeded(fresh, null));

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        Assert.AreSame(sourceSide, after.ActiveSide);
        Assert.AreSame(before.Of(sourceSide), after.Of(sourceSide));
        Assert.AreSame(fresh, Listed(after.Of(sourceSide.Other)).Listing);
        Assert.AreSame(fresh.Entries[0].Path, Listed(after.Of(sourceSide.Other)).State.FocusItem);
        Assert.HasCount(2, destination.Requests);
        Assert.AreSame(Listed(before.Of(sourceSide)).State.Location, destination.Requests[1].Location);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    private static PaneContentListed Listed(PaneSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content);
    }

    private static Task<DualPaneSnapshot> HandleAsync(DualPaneSessionTestContext fixture, UserIntent intent)
    {
        return fixture.Panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
    }
}
