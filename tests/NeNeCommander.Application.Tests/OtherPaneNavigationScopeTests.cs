using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Application.Wsl;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves transient input scopes cannot dispatch opposite-pane navigation through their owner.</summary>
[TestClass]
public sealed class OtherPaneNavigationScopeTests
{
    /// <summary>Proves each input-owning scope preserves its snapshots and refuses a direct pane command.</summary>
    [TestMethod]
    [DataRow("settings")]
    [DataRow("bookmarks")]
    [DataRow("palette")]
    [DataRow("address")]
    [DataRow("locations")]
    [DataRow("help")]
    [DataRow("window")]
    public async Task HandleAsyncWhenScopeOwnsInputKeepsOppositeCommandAwayFromPanesAsync(string scope)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        ScriptedDirectoryReadPort left = fixture.Left;
        ScriptedDirectoryReadPort right = fixture.Right;
        ScriptedDriveCatalog drives = new();
        ScriptedWslDistributionCatalog wsl = new();
        SettingsSession bookmarkSettings = new(new ScriptedSettingsStore(SettingsReadOutcome.Absent()), SettingsReadOutcome.Absent(), static _ => { });
        CommanderSession session = new(fixture.Panes, bookmarkSettings, new TransientScopeOwners(new PaneScopeOwners(new AddressEditorSession(), new LocationsSession(drives, wsl), new PaneFilterSession()), new CommandPaletteSession(), new WindowAdjustmentSession(), new KeyboardHelpSession()), new CommanderBookmarkCommands(fixture.Panes, bookmarkSettings));
        DirectoryListing listing = DualPaneSessionTestContext.Listing(@"C:\source", ("source.txt", DirectoryEntryKind.File));
        left.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        _ = await session.NavigateAsync(PaneSide.Left, listing.Location, CancellationToken.None);
        RecordingCommanderObserver observer = new();
        Task<CommanderSnapshot> opening = session.HandleAsync(OpenIntent(scope), observer, CancellationToken.None);
        CommanderSnapshot before = session.Current;

        CommanderSnapshot after = await session.HandleAsync(UserIntent.OpenCurrentLocationInOtherPane, observer, CancellationToken.None);

        Assert.AreSame(before.Panes.Left, after.Panes.Left);
        Assert.AreSame(before.Panes.Right, after.Panes.Right);
        Assert.AreEqual(before.Settings, after.Settings);
        Assert.AreEqual(before.Scopes, after.Scopes);
        Assert.HasCount(1, left.Requests);
        Assert.IsEmpty(right.Requests);
        if (scope == "locations")
        {
            _ = Assert.IsInstanceOfType<LocationsLoading>(after.Scopes.Locations);
            drives.Complete(DriveCatalogOutcome.Succeeded([], 0));
            wsl.Complete(WslDistributionCatalogOutcome.Succeeded([]));
        }
        _ = await opening;
    }

    private static UserIntent OpenIntent(string scope)
    {
        return scope switch
        {
            "settings" => UserIntent.OpenSettings,
            "bookmarks" => UserIntent.OpenBookmarks,
            "palette" => UserIntent.OpenCommandPalette,
            "address" => UserIntent.FocusAddress,
            "locations" => UserIntent.OpenLocations,
            "help" => UserIntent.OpenHelp,
            _ => UserIntent.OpenWindowAdjustment,
        };
    }
}
