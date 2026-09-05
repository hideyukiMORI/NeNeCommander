using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the deterministic projection of pane snapshots onto rows, focus, status, and address.</summary>
[TestClass]
public sealed class PaneListingPresenterTests
{
    /// <summary>Proves a pane that never read a location shows nothing and says so.</summary>
    [TestMethod]
    public void PresentWhenNothingIsListedShowsNoListingStatus()
    {
        PanePresentation presentation = PaneListingPresenter.Present(PaneSnapshot.Initial, PaneFrame.Active);

        Assert.IsEmpty(presentation.Rows);
        Assert.IsNull(presentation.FocusRow);
        Assert.AreSame(PaneStatus.NoListing, presentation.Status);
        Assert.AreEqual(string.Empty, presentation.AddressText);
    }

    /// <summary>Proves a listed location shows its ordered rows, focus entry, and address.</summary>
    [TestMethod]
    public async Task PresentWhenLocationIsListedShowsRowsFocusAndAddress()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = CreateListing("C:\\projects", ["b.txt", "a.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        PaneSession session = CreateSession(port);
        PaneSnapshot snapshot = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PanePresentation initial = PaneListingPresenter.Present(snapshot, PaneFrame.Active);
        PanePresentation afterMove = PaneListingPresenter.Present(moved, PaneFrame.Active);

        Assert.HasCount(2, initial.Rows);
        Assert.AreSame(listing.Entries[0], initial.Rows[0].Entry);
        Assert.AreSame(listing.Entries[1], initial.Rows[1].Entry);
        Assert.AreSame(initial.Rows[0], initial.FocusRow);
        Assert.AreSame(listing.Entries[1], afterMove.FocusRow?.Entry);
        Assert.AreSame(PaneRowMark.FocusInActivePane, initial.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.Unmarked, initial.Rows[1].Mark);
        Assert.AreSame(PaneRowKind.File, initial.Rows[0].Kind);
        Assert.AreSame(PaneStatus.Complete, initial.Status);
        Assert.AreEqual("C:\\projects", initial.AddressText);
    }

    /// <summary>Proves selected rows carry the selected mark by provider identity and escape clears it.</summary>
    [TestMethod]
    public async Task PresentWhenItemsAreSelectedMarksOnlyThoseRows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = CreateListing("C:\\projects", ["a.txt", "b.txt", "c.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        PaneSnapshot selected = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        PaneSnapshot cleared = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);

        PanePresentation marked = PaneListingPresenter.Present(selected, PaneFrame.Active);
        PanePresentation unmarked = PaneListingPresenter.Present(cleared, PaneFrame.Active);

        Assert.AreSame(PaneRowMark.Selected, marked.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.Unmarked, marked.Rows[1].Mark);
        Assert.AreSame(PaneRowMark.FocusInActivePane, marked.Rows[2].Mark);
        Assert.AreSame(marked.Rows[2], marked.FocusRow);
        Assert.AreSame(PaneRowMark.Unmarked, unmarked.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.FocusInActivePane, unmarked.Rows[2].Mark);
        Assert.AreSame(unmarked.Rows[2], unmarked.FocusRow);
    }
    /// <summary>Proves an empty listing has no focus entry.</summary>
    [TestMethod]
    public async Task PresentWhenListingIsEmptyHasNoFocusEntry()
    {
        PaneSnapshot snapshot = await ListAsync(CreateListing("C:\\projects", [], DirectoryListingCompleteness.Complete, 0));

        PanePresentation presentation = PaneListingPresenter.Present(snapshot, PaneFrame.Active);

        Assert.IsNull(presentation.FocusRow);
        Assert.IsEmpty(presentation.Rows);
    }

    /// <summary>Proves listing completeness maps to bounded before omitted before complete.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public async Task PresentWhenListingIsBoundedOrOmittedReportsExactStatus()
    {
        PaneSnapshot bounded = await ListAsync(CreateListing("C:\\a", ["a.txt"], DirectoryListingCompleteness.Bounded, 2));
        PaneSnapshot omitted = await ListAsync(CreateListing("C:\\b", ["a.txt"], DirectoryListingCompleteness.Complete, 1));

        Assert.AreSame(PaneStatus.Bounded, PaneListingPresenter.Present(bounded, PaneFrame.Active).Status);
        Assert.AreSame(PaneStatus.EntriesOmitted, PaneListingPresenter.Present(omitted, PaneFrame.Active).Status);
    }

    /// <summary>Proves a read in flight keeps the rows and shows the target as loading.</summary>
    [TestMethod]
    public async Task PresentWhenReadIsInFlightKeepsRowsAndShowsLoading()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = CreateListing("C:\\projects", ["a.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\next"), CancellationToken.None);

        PanePresentation listedLoading = PaneListingPresenter.Present(session.Current, PaneFrame.Active);
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await navigation;
        PanePresentation absentLoading = PaneListingPresenter.Present(
            CreateSessionWithPendingRead(out TaskCompletionSource<DirectoryReadOutcome> release).Current,
            PaneFrame.Active);
        release.SetResult(DirectoryReadOutcome.Cancelled());

        Assert.AreSame(listing.Entries[0], listedLoading.Rows[0].Entry);
        Assert.AreSame(PaneStatus.Loading, listedLoading.Status);
        Assert.AreEqual("C:\\projects", listedLoading.AddressText);
        Assert.IsEmpty(absentLoading.Rows);
        Assert.AreSame(PaneStatus.Loading, absentLoading.Status);
        Assert.AreEqual("C:\\pending", absentLoading.AddressText);
    }

    /// <summary>Proves an activity-only update keeps the existing row source and row instances.</summary>
    [TestMethod]
    public async Task PresentWhenOnlyActivityChangesReusesRows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = CreateListing(
            "C:\\projects",
            ["a.txt", "b.txt"],
            DirectoryListingCompleteness.Complete,
            0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(listing.Location, CancellationToken.None);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\next"), CancellationToken.None);
        PaneSnapshot loading = session.Current;
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await navigation;
        PanePresentation initial = PaneListingPresenter.Present(listed, PaneFrame.Active);

        PanePresentation updated = PaneListingPresenter.Present(loading, PaneFrame.Active, initial);

        Assert.AreSame(initial.Rows, updated.Rows);
        Assert.AreSame(initial.Rows[0], updated.Rows[0]);
        Assert.AreSame(initial.Rows[1], updated.Rows[1]);
        Assert.AreSame(PaneStatus.Loading, updated.Status);
    }

    /// <summary>Proves focus movement replaces only the previous and current focus rows.</summary>
    [TestMethod]
    public async Task PresentWhenFocusMovesReplacesOnlyAffectedRows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = CreateListing(
            "C:\\projects",
            ["a.txt", "b.txt", "c.txt"],
            DirectoryListingCompleteness.Complete,
            0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        PaneSession session = CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PanePresentation initial = PaneListingPresenter.Present(listed, PaneFrame.Active);
        PaneRow priorFocus = initial.Rows[0];
        PaneRow priorNext = initial.Rows[1];
        PaneRow unaffected = initial.Rows[2];
        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PanePresentation updated = PaneListingPresenter.Present(moved, PaneFrame.Active, initial);

        Assert.AreSame(initial.Rows, updated.Rows);
        Assert.AreNotSame(priorFocus, updated.Rows[0]);
        Assert.AreNotSame(priorNext, updated.Rows[1]);
        Assert.AreSame(unaffected, updated.Rows[2]);
        Assert.AreSame(PaneRowMark.Unmarked, updated.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.FocusInActivePane, updated.Rows[1].Mark);
    }

    /// <summary>Proves cancellation and each failure map to one closed status with the target address.</summary>
    [TestMethod]
    public async Task PresentWhenReadIsCancelledOrFailsTranslatesActivity()
    {
        PanePresentation cancelled = PaneListingPresenter.Present(
            await ReadAbsentAsync(DirectoryReadOutcome.Cancelled()),
            PaneFrame.Active);
        PanePresentation denied = PaneListingPresenter.Present(
            await ReadAbsentAsync(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied)),
            PaneFrame.Active);
        PanePresentation missing = PaneListingPresenter.Present(
            await ReadAbsentAsync(DirectoryReadOutcome.Failed(FileOperationFailureKind.NotFound)),
            PaneFrame.Active);
        PanePresentation unavailable = PaneListingPresenter.Present(
            await ReadAbsentAsync(DirectoryReadOutcome.Failed(FileOperationFailureKind.Copy)),
            PaneFrame.Active);

        Assert.AreSame(PaneStatus.Cancelled, cancelled.Status);
        Assert.AreEqual("C:\\target", cancelled.AddressText);
        Assert.AreSame(PaneStatus.AccessDenied, denied.Status);
        Assert.AreEqual("C:\\target", denied.AddressText);
        Assert.AreSame(PaneStatus.NotFound, missing.Status);
        Assert.AreSame(PaneStatus.ProviderUnavailable, unavailable.Status);
        Assert.IsEmpty(unavailable.Rows);
    }

    /// <summary>Proves a failed read over listed content keeps the rows and the listed address.</summary>
    [TestMethod]
    public async Task PresentWhenReadFailsOverListedContentKeepsRows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = CreateListing("C:\\projects", ["a.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        port.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.NotFound));
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneSnapshot snapshot = await session.NavigateAsync(ParsePath("C:\\missing"), CancellationToken.None);

        PanePresentation presentation = PaneListingPresenter.Present(snapshot, PaneFrame.Active);

        Assert.AreSame(listing.Entries[0], presentation.Rows[0].Entry);
        Assert.AreSame(presentation.Rows[0], presentation.FocusRow);
        Assert.AreSame(PaneStatus.NotFound, presentation.Status);
        Assert.AreEqual("C:\\projects", presentation.AddressText);
    }

    /// <summary>Proves the passive pane marks its focus row differently and selection outranks it.</summary>
    [TestMethod]
    public async Task PresentWhenPaneIsPassiveMarksFocusRowWithoutTheActiveTreatment()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = CreateListing("C:\\projects", ["a.txt", "b.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneSnapshot focused = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);

        PanePresentation passive = PaneListingPresenter.Present(focused, PaneFrame.Passive);
        PanePresentation active = PaneListingPresenter.Present(focused, PaneFrame.Active);

        Assert.AreSame(PaneRowMark.Selected, passive.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.FocusInActivePane, active.Rows[0].Mark);
        Assert.AreSame(passive.Rows[0], passive.FocusRow);
    }

    /// <summary>Proves an unselected focus row of a passive pane keeps the passive focus mark.</summary>
    [TestMethod]
    public async Task PresentWhenPassivePaneFocusIsUnselectedMarksPassiveFocus()
    {
        PaneSnapshot snapshot = await ListAsync(
            CreateListing("C:\\projects", ["a.txt", "b.txt"], DirectoryListingCompleteness.Complete, 0));

        PanePresentation passive = PaneListingPresenter.Present(snapshot, PaneFrame.Passive);

        Assert.AreSame(PaneRowMark.FocusInPassivePane, passive.Rows[0].Mark);
        Assert.AreSame(PaneRowMark.Unmarked, passive.Rows[1].Mark);
    }

    /// <summary>Proves every row mark and entry kind names the exact semantic design resources.</summary>
    [TestMethod]
    public void ResourceKeyWhenRowMarkOrKindIsReadNamesExactResources()
    {
        Assert.AreEqual("FocusRingBrush", PaneRowMark.FocusInActivePane.MarkerBrushResourceKey);
        Assert.AreEqual("FocusSurfaceBrush", PaneRowMark.FocusInActivePane.SurfaceBrushResourceKey);
        Assert.AreEqual("SelectionMarkBrush", PaneRowMark.Selected.MarkerBrushResourceKey);
        Assert.AreEqual("SelectionSurfaceBrush", PaneRowMark.Selected.SurfaceBrushResourceKey);
        Assert.AreEqual("BorderSubtleBrush", PaneRowMark.FocusInPassivePane.MarkerBrushResourceKey);
        Assert.AreEqual("SurfacePaneBrush", PaneRowMark.FocusInPassivePane.SurfaceBrushResourceKey);
        Assert.AreEqual("SurfacePaneBrush", PaneRowMark.Unmarked.MarkerBrushResourceKey);
        Assert.AreEqual("SurfacePaneBrush", PaneRowMark.Unmarked.SurfaceBrushResourceKey);
        Assert.AreEqual("EntryKindDirectoryLabel", PaneRowKind.Directory.LabelResourceKey);
        Assert.IsTrue(PaneRowKind.Directory.IsDirectory);
        Assert.IsFalse(PaneRowKind.Directory.IsFile);
        Assert.AreEqual("EntryKindFileLabel", PaneRowKind.File.LabelResourceKey);
        Assert.IsFalse(PaneRowKind.File.IsDirectory);
        Assert.IsTrue(PaneRowKind.File.IsFile);
        Assert.AreSame(PaneRowKind.Directory, PaneRowKind.For(DirectoryEntryKind.Directory));
        Assert.AreSame(PaneRowKind.File, PaneRowKind.For(DirectoryEntryKind.File));
        Assert.AreEqual("FocusRingBrush", PaneFrame.Active.NumberSurfaceBrushResourceKey);
        Assert.AreEqual("SurfacePaneBrush", PaneFrame.Active.NumberForegroundBrushResourceKey);
        Assert.AreEqual("BorderSubtleBrush", PaneFrame.Passive.NumberSurfaceBrushResourceKey);
        Assert.AreEqual("TextSecondaryBrush", PaneFrame.Passive.NumberForegroundBrushResourceKey);
    }

    /// <summary>Proves the presenter rejects an absent snapshot.</summary>
    [TestMethod]
    public void PresentWhenSnapshotIsNullThrowsArgumentNullException()
    {
        MethodInfo method = typeof(PaneListingPresenter).GetMethod(
            nameof(PaneListingPresenter.Present),
            BindingFlags.Public | BindingFlags.Static) ??
            throw new AssertFailedException("The present method was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [null, PaneFrame.Active]));
        TargetInvocationException absentFrame = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [PaneSnapshot.Initial, null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
        _ = Assert.IsInstanceOfType<ArgumentNullException>(absentFrame.InnerException);
    }

    /// <summary>Proves the kind projection rejects an absent entry kind.</summary>
    [TestMethod]
    public void ForWhenEntryKindIsNullThrowsArgumentNullException()
    {
        MethodInfo method = typeof(PaneRowKind).GetMethod(
            nameof(PaneRowKind.For),
            BindingFlags.Public | BindingFlags.Static) ??
            throw new AssertFailedException("The kind projection was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    /// <summary>Proves every status names a distinct localization resource key.</summary>
    [TestMethod]
    public void ResourceKeyWhenStatusIsReadNamesExactResource()
    {
        Assert.AreEqual("PaneStatusNoListing", PaneStatus.NoListing.ResourceKey);
        Assert.AreEqual("PaneStatusLoading", PaneStatus.Loading.ResourceKey);
        Assert.AreEqual("PaneStatusComplete", PaneStatus.Complete.ResourceKey);
        Assert.AreEqual("PaneStatusBounded", PaneStatus.Bounded.ResourceKey);
        Assert.AreEqual("PaneStatusEntriesOmitted", PaneStatus.EntriesOmitted.ResourceKey);
        Assert.AreEqual("PaneStatusAccessDenied", PaneStatus.AccessDenied.ResourceKey);
        Assert.AreEqual("PaneStatusNotFound", PaneStatus.NotFound.ResourceKey);
        Assert.AreEqual("PaneStatusProviderUnavailable", PaneStatus.ProviderUnavailable.ResourceKey);
        Assert.AreEqual("PaneStatusCancelled", PaneStatus.Cancelled.ResourceKey);
    }

    private static async Task<PaneSnapshot> ListAsync(DirectoryListing listing)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        return await CreateSession(port).NavigateAsync(listing.Location, CancellationToken.None);
    }

    private static async Task<PaneSnapshot> ReadAbsentAsync(DirectoryReadOutcome outcome)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(outcome);
        return await CreateSession(port).NavigateAsync(ParsePath("C:\\target"), CancellationToken.None);
    }

    private static PaneSession CreateSessionWithPendingRead(out TaskCompletionSource<DirectoryReadOutcome> release)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        release = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        _ = session.NavigateAsync(ParsePath("C:\\pending"), CancellationToken.None);
        return session;
    }

    private static PaneSession CreateSession(IDirectoryReadPort port)
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(4)).Capacity;
        return new PaneSession(port, capacity, DirectoryListing.EntryBoundaryLimit);
    }

    private static DirectoryListing CreateListing(
        string location,
        string[] names,
        DirectoryListingCompleteness completeness,
        int unrepresentableEntryCount)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] entries = new DirectoryEntry[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            entries[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + "\\" + names[index]),
                names[index],
                DirectoryEntryKind.File);
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            entries,
            completeness,
            unrepresentableEntryCount);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
