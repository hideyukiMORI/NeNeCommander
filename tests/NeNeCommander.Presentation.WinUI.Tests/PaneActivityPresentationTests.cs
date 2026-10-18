using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves PaneActivityPresentation behavior and contracts.</summary>
[TestClass]
public sealed class PaneActivityPresentationTests
{
    /// <summary>Proves a pane that never read a location shows nothing and says so.</summary>
    [TestMethod]
    public void PresentWhenNothingIsListedShowsNoListingStatus()
    {
        PanePresentation presentation = PaneListingPresenter.Present(PaneSnapshot.Initial, PaneFrame.Active, TestMetadataFormats.Utc);

        Assert.IsEmpty(presentation.Rows);
        Assert.IsNull(presentation.FocusRow);
        Assert.AreSame(PaneStatus.NoListing, presentation.Status);
        Assert.AreEqual(string.Empty, presentation.AddressText);
    }

    /// <summary>Proves listing completeness maps to bounded before omitted before complete.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public async Task PresentWhenListingIsBoundedOrOmittedReportsExactStatus()
    {
        PaneSnapshot bounded = await PaneListingTestContext.ListAsync(PaneListingTestContext.CreateListing("C:\\a", ["a.txt"], DirectoryListingCompleteness.Bounded, 2));
        PaneSnapshot omitted = await PaneListingTestContext.ListAsync(PaneListingTestContext.CreateListing("C:\\b", ["a.txt"], DirectoryListingCompleteness.Complete, 1));

        Assert.AreSame(PaneStatus.Bounded, PaneListingPresenter.Present(bounded, PaneFrame.Active, TestMetadataFormats.Utc).Status);
        Assert.AreSame(PaneStatus.EntriesOmitted, PaneListingPresenter.Present(omitted, PaneFrame.Active, TestMetadataFormats.Utc).Status);
    }

    /// <summary>Proves a read in flight keeps the rows and shows the target as loading.</summary>
    [TestMethod]
    public async Task PresentWhenReadIsInFlightKeepsRowsAndShowsLoading()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneListingTestContext.CreateListing("C:\\projects", ["a.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = PaneListingTestContext.CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        Task<PaneSnapshot> navigation = session.NavigateAsync(PaneListingTestContext.ParsePath("C:\\next"), CancellationToken.None);

        PanePresentation listedLoading = PaneListingPresenter.Present(session.Current, PaneFrame.Active, TestMetadataFormats.Utc);
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await navigation;
        PanePresentation absentLoading = PaneListingPresenter.Present(
            PaneListingTestContext.CreateSessionWithPendingRead(out TaskCompletionSource<DirectoryReadOutcome> release).Current,
            PaneFrame.Active, TestMetadataFormats.Utc);
        release.SetResult(DirectoryReadOutcome.Cancelled());

        Assert.AreSame(listing.Entries[0], listedLoading.Rows[0].Entry);
        Assert.AreSame(PaneStatus.Loading, listedLoading.Status);
        Assert.AreEqual("C:\\projects", listedLoading.AddressText);
        Assert.IsEmpty(absentLoading.Rows);
        Assert.AreSame(PaneStatus.Loading, absentLoading.Status);
        Assert.AreEqual("C:\\pending", absentLoading.AddressText);
    }

    /// <summary>Proves an abandoned read maps to its own closed status, not a failure or cancellation, with or without a listing.</summary>
    [TestMethod]
    public async Task PresentWhenReadIsAbandonedTranslatesActivity()
    {
        PaneSession unlisted = PaneListingTestContext.CreateSessionWithPendingRead(out TaskCompletionSource<DirectoryReadOutcome> unlistedRead);
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            PaneListingTestContext.CreateListing("C:\\root", ["a.txt", "b.txt"], DirectoryListingCompleteness.Complete, 0), null));
        TaskCompletionSource<DirectoryReadOutcome> listedRead = port.EnqueuePending();
        PaneSession listed = PaneListingTestContext.CreateSession(port);
        _ = await listed.NavigateAsync(PaneListingTestContext.ParsePath("C:\\root"), CancellationToken.None);
        _ = listed.NavigateAsync(PaneListingTestContext.ParsePath("C:\\unreachable"), CancellationToken.None);

        PanePresentation absent = PaneListingPresenter.Present(
            await unlisted.HandleAsync(UserIntent.Escape, CancellationToken.None),
            PaneFrame.Active, TestMetadataFormats.Utc);
        PanePresentation kept = PaneListingPresenter.Present(
            await listed.HandleAsync(UserIntent.Escape, CancellationToken.None),
            PaneFrame.Active, TestMetadataFormats.Utc);

        Assert.AreSame(PaneStatus.ReadAbandoned, absent.Status);
        Assert.AreEqual("C:\\pending", absent.AddressText);
        Assert.IsEmpty(absent.Rows);
        Assert.AreSame(PaneStatus.ReadAbandoned, kept.Status);
        Assert.HasCount(2, kept.Rows);
        unlistedRead.SetResult(DirectoryReadOutcome.Cancelled());
        listedRead.SetResult(DirectoryReadOutcome.Cancelled());
    }

    /// <summary>Proves cancellation and each failure map to one closed status with the target address.</summary>
    [TestMethod]
    public async Task PresentWhenReadIsCancelledOrFailsTranslatesActivity()
    {
        PanePresentation cancelled = PaneListingPresenter.Present(
            await PaneListingTestContext.ReadAbsentAsync(DirectoryReadOutcome.Cancelled()),
            PaneFrame.Active, TestMetadataFormats.Utc);
        PanePresentation denied = PaneListingPresenter.Present(
            await PaneListingTestContext.ReadAbsentAsync(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied)),
            PaneFrame.Active, TestMetadataFormats.Utc);
        PanePresentation missing = PaneListingPresenter.Present(
            await PaneListingTestContext.ReadAbsentAsync(DirectoryReadOutcome.Failed(FileOperationFailureKind.NotFound)),
            PaneFrame.Active, TestMetadataFormats.Utc);
        PanePresentation unavailable = PaneListingPresenter.Present(
            await PaneListingTestContext.ReadAbsentAsync(DirectoryReadOutcome.Failed(FileOperationFailureKind.Copy)),
            PaneFrame.Active, TestMetadataFormats.Utc);

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
        DirectoryListing listing = PaneListingTestContext.CreateListing("C:\\projects", ["a.txt"], DirectoryListingCompleteness.Complete, 0);
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        port.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.NotFound));
        PaneSession session = PaneListingTestContext.CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneSnapshot snapshot = await session.NavigateAsync(PaneListingTestContext.ParsePath("C:\\missing"), CancellationToken.None);

        PanePresentation presentation = PaneListingPresenter.Present(snapshot, PaneFrame.Active, TestMetadataFormats.Utc);

        Assert.AreSame(listing.Entries[0], presentation.Rows[0].Entry);
        Assert.AreSame(presentation.Rows[0], presentation.FocusRow);
        Assert.AreSame(PaneStatus.NotFound, presentation.Status);
        Assert.AreEqual("C:\\projects", presentation.AddressText);
    }

    /// <summary>Proves a pane that lists nothing has no sort indication.</summary>
    [TestMethod]
    public void PresentWhenNothingIsListedHasNoSortStatus()
    {
        PanePresentation presentation = PaneListingPresenter.Present(PaneSnapshot.Initial, PaneFrame.Active, TestMetadataFormats.Utc);

        Assert.IsNull(presentation.SortStatus);
    }

    /// <summary>Proves every file handoff activity projects to its closed localized status.</summary>
    [TestMethod]
    [DataRow("C:\\root")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home")]
    [DataRow("\\\\server\\share\\folder")]
    public async Task PresentWhenFileLaunchChangesActivityTranslatesStatus(string location)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            PaneListingTestContext.CreateListing(location, ["a.txt"], DirectoryListingCompleteness.Complete, 0), null));
        ScriptedFileLauncher launcher = new();
        TaskCompletionSource<FileLaunchOutcome> release = launcher.EnqueuePending();
        launcher.Enqueue(FileLaunchOutcome.Cancelled());
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.NotFound));
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.AccessDenied));
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.AssociationUnavailable));
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.ShellRejected));
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.ProviderUnavailable));
        PaneSession session = PaneListingTestContext.CreateSession(port, launcher);
        _ = await session.NavigateAsync(PaneListingTestContext.ParsePath(location), CancellationToken.None);

        Task<PaneSnapshot> pending = session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);
        Assert.AreSame(PaneStatus.Launching, PaneListingTestContext.Present(session.Current).Status);
        release.SetResult(FileLaunchOutcome.Accepted());
        _ = await pending;
        Assert.AreSame(PaneStatus.LaunchCancelled, PaneListingTestContext.Present(
            await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None)).Status);
        Assert.AreSame(PaneStatus.LaunchNotFound, PaneListingTestContext.Present(
            await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None)).Status);
        Assert.AreSame(PaneStatus.LaunchAccessDenied, PaneListingTestContext.Present(
            await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None)).Status);
        Assert.AreSame(PaneStatus.AssociationUnavailable, PaneListingTestContext.Present(
            await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None)).Status);
        Assert.AreSame(PaneStatus.LaunchUnavailable, PaneListingTestContext.Present(
            await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None)).Status);
        Assert.AreSame(PaneStatus.LaunchUnavailable, PaneListingTestContext.Present(
            await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None)).Status);
    }
}
