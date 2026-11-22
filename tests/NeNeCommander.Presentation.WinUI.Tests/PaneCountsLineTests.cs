using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves localized counts distinguish loaded, visible, unavailable and retained listings.</summary>
[TestClass]
public sealed class PaneCountsLineTests
{
    /// <summary>Proves a missing listing and a successful empty listing have distinct text.</summary>
    [TestMethod]
    public async Task ComposeAsyncWhenListingIsAbsentOrEmptyDistinguishesUnknownFromZero()
    {
        PanePresentation absent = PaneListingTestContext.Present(PaneSnapshot.Initial);
        PanePresentation empty = PaneListingTestContext.Present(await PaneListingTestContext.ListAsync(
            PaneListingTestContext.CreateListing("C:\\empty", [], DirectoryListingCompleteness.Complete, 0)));

        string unavailable = PaneCountsLine.Compose(absent, Localize);
        string known = PaneCountsLine.Compose(empty, Localize);

        Assert.AreEqual("Counts unavailable", unavailable);
        Assert.AreEqual("Files 0 / Folders 0 / Selected 0", known);
    }

    /// <summary>Proves hidden rows are described as loaded, without inflating the shown count.</summary>
    [TestMethod]
    public async Task ComposeAsyncWhenHiddenRowsAreExcludedNamesBothCountBases()
    {
        PanePresentation presentation = PaneListingTestContext.Present(await PaneListingTestContext.ListAsync(
            PaneListingTestContext.CreateMixedListing("C:\\root")));

        string text = PaneCountsLine.Compose(presentation, Localize);

        Assert.AreEqual("Shown 1 files, 0 folders / Selected 0 / Loaded 2 files, 0 folders", text);
    }

    /// <summary>Proves an incomplete admission is never described as a whole-directory total.</summary>
    [TestMethod]
    [DataRow("bounded", 0)]
    [DataRow("complete", 2)]
    [DataRow("bounded", 2)]
    public async Task ComposeAsyncWhenListingIsPartialQualifiesLoadedCounts(string completeness, int omitted)
    {
        DirectoryListing listing = PaneListingTestContext.CreateListing("C:\\root", ["a"],
            completeness == "bounded" ? DirectoryListingCompleteness.Bounded : DirectoryListingCompleteness.Complete, omitted);
        PanePresentation presentation = PaneListingTestContext.Present(await PaneListingTestContext.ListAsync(listing));

        string text = PaneCountsLine.Compose(presentation, Localize);

        Assert.AreEqual("Loaded only: Files 1 / Folders 0 / Selected 0", text);
    }

    /// <summary>Proves pending navigation qualifies the old displayed counts until the new listing arrives.</summary>
    [TestMethod]
    public async Task ComposeAsyncWhenNewLocationIsLoadingQualifiesPreviousCounts()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = PaneListingTestContext.CreateMeasuredListing("C:\\old");
        DirectoryListing after = PaneListingTestContext.CreateListing("C:\\new", ["only"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PanePresentation first = PaneListingTestContext.Present(await session.NavigateAsync(before.Location, CancellationToken.None));
        Task<PaneSnapshot> navigation = session.NavigateAsync(after.Location, CancellationToken.None);
        PanePresentation loading = PaneListingPresenter.Present(session.Current, PaneFrame.Active, TestMetadataFormats.Utc, first);

        string pendingText = PaneCountsLine.Compose(loading, Localize);
        pending.SetResult(DirectoryReadOutcome.Succeeded(after, null));
        PaneSnapshot finished = await navigation;
        PanePresentation current = PaneListingPresenter.Present(finished, PaneFrame.Active, TestMetadataFormats.Utc, loading);

        Assert.AreSame(first.Counts, loading.Counts);
        Assert.AreEqual("C:\\old", loading.AddressText);
        Assert.AreEqual("Last listing: Files 2 / Folders 1 / Selected 0", pendingText);
        Assert.AreEqual("C:\\new", current.AddressText);
        Assert.AreEqual("Files 1 / Folders 0 / Selected 0", PaneCountsLine.Compose(current, Localize));
        Assert.AreEqual(3, Assert.IsInstanceOfType<PaneCounts>(first.Counts).Loaded.Total);
    }

    /// <summary>Proves failed and cancelled refreshes keep qualified prior counts.</summary>
    [TestMethod]
    public async Task ComposeAsyncWhenRefreshFailsOrCancelsQualifiesRetainedCounts()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateMeasuredListing("C:\\old");
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        port.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        port.Enqueue(DirectoryReadOutcome.Cancelled());
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PanePresentation first = PaneListingTestContext.Present(await session.NavigateAsync(listing.Location, CancellationToken.None));

        PanePresentation failed = PaneListingPresenter.Present(await session.RefreshAsync(CancellationToken.None), PaneFrame.Active, TestMetadataFormats.Utc, first);
        PanePresentation cancelled = PaneListingPresenter.Present(await session.RefreshAsync(CancellationToken.None), PaneFrame.Active, TestMetadataFormats.Utc, failed);

        Assert.AreSame(first.Counts, failed.Counts);
        Assert.AreSame(first.Counts, cancelled.Counts);
        Assert.AreEqual("Last listing: Files 2 / Folders 1 / Selected 0", PaneCountsLine.Compose(failed, Localize));
        Assert.AreEqual("Last listing: Files 2 / Folders 1 / Selected 0", PaneCountsLine.Compose(cancelled, Localize));
    }

    /// <summary>Proves an abandoned refresh keeps qualified counts and discards the late listing.</summary>
    [TestMethod]
    public async Task ComposeAsyncWhenReadIsAbandonedKeepsLastListingCounts()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing before = PaneListingTestContext.CreateMeasuredListing("C:\\old");
        port.Enqueue(DirectoryReadOutcome.Succeeded(before, null));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = PaneListingTestContext.CreateSession(port);
        PanePresentation first = PaneListingTestContext.Present(await session.NavigateAsync(before.Location, CancellationToken.None));
        Task<PaneSnapshot> refresh = session.RefreshAsync(CancellationToken.None);

        PaneSnapshot abandoned = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);
        _ = await refresh;
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        PanePresentation current = PaneListingPresenter.Present(abandoned, PaneFrame.Active, TestMetadataFormats.Utc, first);

        Assert.AreSame(first.Counts, current.Counts);
        Assert.AreEqual("Last listing: Files 2 / Folders 1 / Selected 0", PaneCountsLine.Compose(current, Localize));
    }

    /// <summary>Proves the listing boundary formats as loaded counts rather than an exact directory total.</summary>
    [TestMethod]
    public async Task ComposeAsyncWhenListingReachesBoundaryShowsGroupedLoadedCounts()
    {
        string[] names = [.. Enumerable.Range(0, DirectoryListing.EntryBoundaryLimit).Select(index => "file" + index)];
        DirectoryListing listing = PaneListingTestContext.CreateListing("C:\\root", names, DirectoryListingCompleteness.Bounded, 0);
        PanePresentation presentation = PaneListingTestContext.Present(await PaneListingTestContext.ListAsync(listing));

        string text = PaneCountsLine.Compose(presentation, Localize);

        Assert.AreEqual("Loaded only: Files 10,000 / Folders 0 / Selected 0", text);
    }

    /// <summary>Proves required formatter dependencies are validated before resource access.</summary>
    [TestMethod]
    public void ComposeWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        PanePresentation absent = PaneListingTestContext.Present(PaneSnapshot.Initial);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneCountsLine.Compose(null!, Localize));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneCountsLine.Compose(absent, null!));
    }

    private static string Localize(string key)
    {
        return key switch
        {
            "PaneCountsUnavailable" => "Counts unavailable",
            "PaneCountsFormat" => "Files {0:N0} / Folders {1:N0} / Selected {2:N0}",
            "PaneCountsVisibleFormat" => "Shown {0:N0} files, {1:N0} folders / Selected {2:N0} / Loaded {3:N0} files, {4:N0} folders",
            "PaneCountsLoadedOnlyFormat" => "Loaded only: {0}",
            "PaneCountsPreviousFormat" => "Last listing: {0}",
            _ => throw new AssertFailedException("Unexpected count resource: " + key),
        };
    }
}
