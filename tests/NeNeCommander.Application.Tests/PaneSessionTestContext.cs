using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Builds deterministic pane-session inputs and reads their retained navigation state.</summary>
internal static class PaneSessionTestContext
{
    internal static DirectoryListing HiddenListing(string location, params (string Name, EntryVisibility Visibility)[] entries)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] built = new DirectoryEntry[entries.Length];
        for (int index = 0; index < entries.Length; index++)
        {
            built[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + "\\" + entries[index].Name),
                entries[index].Name,
                DirectoryEntryKind.File,
                EntryMetadata.Unmeasured(entries[index].Visibility));
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            built,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    internal static PaneSession CreateSession(IDirectoryReadPort port)
    {
        return CreateSession(port, HiddenItemVisibility.Hidden);
    }

    internal static PaneSession CreateSession(IDirectoryReadPort port, HiddenItemVisibility visibility)
    {
        return CreateSession(port, new ScriptedFileLauncher(), visibility);
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
        return new PaneSession(port,
            fileLauncher,
            PaneSessionConfiguration.Create(Capacity(4), DirectoryListing.EntryBoundaryLimit, visibility, PaneSortOrder.Default));
    }

    internal static void AssertVisibleNames(string[] expected, PaneState state)
    {
        CollectionAssert.AreEqual(expected, state.VisibleEntries.Select(entry => entry.Name).ToArray());
    }

    internal static PaneNavigationHistory HistoryOf(PaneSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content).State.NavigationHistory;
    }

    internal static VisiblePageCapacity Capacity(int rows)
    {
        return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(rows)).Capacity;
    }

    internal static DirectoryListing Listing(string location, params (string Name, DirectoryEntryKind Kind)[] entries)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] built = new DirectoryEntry[entries.Length];
        for (int index = 0; index < entries.Length; index++)
        {
            string separator = parsedLocation.CanonicalText.EndsWith('\\') ? string.Empty : "\\";
            built[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + separator + entries[index].Name),
                entries[index].Name,
                entries[index].Kind,
                EntryMetadata.Unmeasured(EntryVisibility.Normal));
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            built,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    internal static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
