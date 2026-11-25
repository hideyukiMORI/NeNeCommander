using System;
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

/// <summary>Composes context-selection tests over the existing owned pane fixture and real scope owners.</summary>
internal sealed class CommanderContextTestContext : IDisposable
{
    private CommanderContextTestContext(DualPaneSessionTestContext panes)
    {
        Panes = panes;
        Palette = new CommandPaletteSession();
        SettingsSession bookmarkSettings = new(new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
                SettingsReadOutcome.Absent(), static _ => { });
        Session = new CommanderSession(panes.Panes, bookmarkSettings, new TransientScopeOwners(new PaneScopeOwners(new AddressEditorSession(), new LocationsSession(
                    ScriptedDriveCatalog.Completed(DriveCatalogOutcome.Succeeded([], 0)),
                    ScriptedWslDistributionCatalog.Completed(WslDistributionCatalogOutcome.Succeeded([]))), new PaneFilterSession()), Palette, new WindowAdjustmentSession(), new KeyboardHelpSession()), new CommanderBookmarkCommands(panes.Panes, bookmarkSettings));
    }

    internal CommanderSession Session { get; }

    internal CommandPaletteSession Palette { get; }

    internal DualPaneSessionTestContext Panes { get; }

    internal static CommanderContextTestContext Create()
    {
        return new CommanderContextTestContext(DualPaneSessionTestContext.Create());
    }

    internal Task ListBothAsync()
    {
        return Panes.ListBothAsync(Listing("C:\\left"), Listing("C:\\right"));
    }

    internal static DirectoryListing Listing(string location)
    {
        return DualPaneSessionTestContext.Listing(location,
            ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File),
            ("c.txt", DirectoryEntryKind.File));
    }

    internal Task<CommanderSnapshot> HandleAsync(UserIntent intent)
    {
        return Session.HandleAsync(intent, new RecordingCommanderObserver(), CancellationToken.None);
    }

    internal Task<CommanderSnapshot> RowAsync(PaneSide side, int index, PaneEntryAction action)
    {
        PaneSnapshot pane = Session.Current.Panes.Of(side);
        return HandleAsync(UserIntent.InteractWithPaneEntry(side, pane,
            StateOf(pane).VisibleEntries[index], action));
    }

    internal Task<CommanderSnapshot> FolderAsync(PaneSide side)
    {
        return HandleAsync(UserIntent.InteractWithPaneSurface(
            side, Session.Current.Panes.Of(side), PaneSurfaceAction.OpenContextMenu));
    }

    internal static PaneState StateOf(PaneSnapshot pane)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(pane.Content).State;
    }

    internal static CommandPaletteOpen OpenOf(CommanderSnapshot snapshot)
    {
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>(snapshot.Scopes.CommandPalette);
        Assert.AreSame(snapshot.Panes.Left, open.Left);
        Assert.AreSame(snapshot.Panes.Right, open.Right);
        Assert.AreSame(snapshot.Panes.ActiveSide, open.ActiveSide);
        return open;
    }

    public void Dispose()
    {
        Panes.Dispose();
    }
}
