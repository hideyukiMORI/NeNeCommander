using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

internal static class CommanderSessionTestValues
{
    internal static PaneNavigationHistory HistoryOf(CommanderSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Panes.Left.Content)
            .State.NavigationHistory;
    }

    internal static CommanderSession CreateSessionWithCatalog(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        BookmarkCatalog catalog)
    {
        return new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession(SettingsReadOutcome.Read(UserSettings.Create(
                DisplayPreferences.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, KeyHintVisibility.Shown),
                catalog, PaneSortPreferences.Default)));
    }

    internal static CommandCandidate Candidate(CommandPaletteOpen palette, UserIntent intent)
    {
        return palette.Candidates.Single(candidate => candidate.Intent == intent);
    }

    internal static void AssertUnavailable(
        CommandPaletteOpen palette,
        UserIntent intent,
        CommandUnavailableReason expected)
    {
        CommandUnavailable unavailable = Assert.IsInstanceOfType<CommandUnavailable>(
            Candidate(palette, intent).Availability);
        Assert.AreSame(expected, unavailable.Reason);
    }

    internal static FileOperationGateway CreateGateway()
    {
        return new FileOperationGateway(ScriptedFileOperationPort.Create(null, null));
    }

    internal static VisiblePageCapacity Capacity()
    {
        return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(4)).Capacity;
    }

    internal static DirectoryListing Listing(string location, string name)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry entry = DirectoryEntry.Create(
            ParsePath(location + "\\" + name),
            name,
            DirectoryEntryKind.File,
            EntryMetadata.Unmeasured(EntryVisibility.Normal));
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(
            DirectoryListing.Create(
                parsedLocation,
                [entry],
                DirectoryListingCompleteness.Complete,
            0)).Listing;
    }

    internal static DirectoryListing EmptyListing(string location)
    {
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(
            ParsePath(location),
            [],
            DirectoryListingCompleteness.Complete,
            0)).Listing;
    }

    internal static FileInspectionOutcome Inspection(FileSystemPath path)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(path, identity.Identity, DeletionCapability.Recycle));
    }

    internal static FileInspectionOutcome PermanentInspection(FileSystemPath path)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(path, identity.Identity, DeletionCapability.PermanentOnly));
    }

    internal static FileSystemPath ParsePath(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    internal static BookmarkEntry Entry(
        string name,
        string path,
        BookmarkShortcutSlot? slot)
    {
        BookmarkDisplayName displayName = Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
            BookmarkDisplayName.Parse(name)).Name;
        BookmarkPath bookmarkPath = Assert.IsInstanceOfType<BookmarkPathAccepted>(
            BookmarkPath.Parse(path)).Path;
        return BookmarkEntry.Create(displayName, bookmarkPath, null, slot);
    }

    internal static BookmarkCatalog Catalog(BookmarkEntry entry)
    {
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create([], [entry])).Catalog;
    }
}
