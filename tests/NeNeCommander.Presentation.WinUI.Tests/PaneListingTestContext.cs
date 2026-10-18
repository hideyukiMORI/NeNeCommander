using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Creates the owned sessions and snapshots used by pane projection tests.</summary>
internal static class PaneListingTestContext
{
    internal static async Task<PaneSnapshot> ListAsync(DirectoryListing listing)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        return await CreateSession(port).NavigateAsync(listing.Location, CancellationToken.None);
    }

    internal static PanePresentation Present(PaneSnapshot snapshot)
    {
        return PaneListingPresenter.Present(snapshot, PaneFrame.Active, TestMetadataFormats.Utc);
    }

    internal static PaneRow RowNamed(PanePresentation presentation, string name)
    {
        foreach (PaneRow row in presentation.Rows)
        {
            if (row.Entry.Name == name)
            {
                return row;
            }
        }
        throw new AssertFailedException("The row was not projected.");
    }

    internal static async Task<PaneSnapshot> ReadAbsentAsync(DirectoryReadOutcome outcome)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(outcome);
        return await CreateSession(port).NavigateAsync(ParsePath("C:\\target"), CancellationToken.None);
    }

    internal static PaneSession CreateSessionWithPendingRead(out TaskCompletionSource<DirectoryReadOutcome> release)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        release = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        _ = session.NavigateAsync(ParsePath("C:\\pending"), CancellationToken.None);
        return session;
    }

    internal static PaneSession CreateSession(IDirectoryReadPort port)
    {
        return CreateSession(port, HiddenItemVisibility.Hidden);
    }

    internal static PaneSession CreateSession(IDirectoryReadPort port, HiddenItemVisibility visibility)
    {
        return CreateSession(port, new AcceptedFileLauncher(), visibility);
    }

    internal static PaneSession CreateSession(IDirectoryReadPort port, IFileLauncher fileLauncher)
    {
        return CreateSession(port, fileLauncher, HiddenItemVisibility.Hidden);
    }

    internal static PaneSession CreateSession(
        IDirectoryReadPort port,
        IFileLauncher fileLauncher,
        HiddenItemVisibility visibility)
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(4)).Capacity;
        return new PaneSession(port,
            fileLauncher,
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, visibility, PaneSortOrder.Default));
    }

    internal static DirectoryListing CreateListing(
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
                DirectoryEntryKind.File,
                EntryMetadata.Unmeasured(EntryVisibility.Normal));
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            entries,
            completeness,
            unrepresentableEntryCount);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    internal static DirectoryListing CreateMeasuredListing(string location)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DateTimeOffset modified = new(2026, 10, 9, 14, 59, 0, TimeSpan.Zero);
        DirectoryEntry[] entries = [
            DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + "\\a.txt"),
                "a.txt",
                DirectoryEntryKind.File,
                EntryMetadata.Create(EntryVisibility.Normal, EntrySize.Create(812), EntryTimestamp.Create(modified))),
            DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + "\\b"),
                "b",
                DirectoryEntryKind.Directory,
                EntryMetadata.Create(EntryVisibility.Normal, EntrySize.Create(4096), EntryTimestamp.Create(modified))),
            DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + "\\c.txt"),
                "c.txt",
                DirectoryEntryKind.File,
                EntryMetadata.Unmeasured(EntryVisibility.Normal)),
        ];
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            entries,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    internal static DirectoryListing CreateMixedListing(string location)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] entries = [
            DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + "\\a.txt"),
                "a.txt",
                DirectoryEntryKind.File,
                EntryMetadata.Unmeasured(EntryVisibility.Normal)),
            DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + "\\b.txt"),
                "b.txt",
                DirectoryEntryKind.File,
                EntryMetadata.Unmeasured(EntryVisibility.Hidden)),
        ];
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            entries,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    internal static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
