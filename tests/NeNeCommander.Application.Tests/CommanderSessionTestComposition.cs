using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Borrowed collaborators used to compose a session without taking gateway ownership.</summary>
internal sealed class CommanderSessionTestComposition
{
    private readonly ScriptedDirectoryReadPort _left;
    private readonly ScriptedDirectoryReadPort _right;
    private readonly FileOperationGateway _gateway;
    private readonly ISettingsStore _store;

    internal CommanderSessionTestComposition(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ISettingsStore store)
    {
        _left = left;
        _right = right;
        _gateway = gateway;
        _store = store;
    }

    internal CommanderSession CreateSession()
    {
        return CreateSession(new ScriptedFileLauncher());
    }

    internal CommanderSession CreateSession(ScriptedFileLauncher leftLauncher)
    {
        return CreateSession(leftLauncher, out _);
    }

    internal CommanderSession CreateSession(out DualPaneSession panes)
    {
        return CreateSession(new ScriptedFileLauncher(), out panes);
    }

    internal CommanderSession CreateSession(ScriptedFileLauncher leftLauncher, out DualPaneSession panes)
    {
        return CreateSession(leftLauncher, new ScriptedFileLauncher(), SettingsReadOutcome.Absent(), out panes);
    }

    internal CommanderSession CreateSession(ScriptedFileLauncher leftLauncher, ScriptedFileLauncher rightLauncher, out DualPaneSession panes)
    {
        return CreateSession(leftLauncher, rightLauncher, SettingsReadOutcome.Absent(), out panes);
    }

    internal CommanderSession CreateSession(SettingsReadOutcome initialOutcome)
    {
        return CreateSession(new ScriptedFileLauncher(), new ScriptedFileLauncher(), initialOutcome, out _);
    }

    internal CommanderSession CreateSession(ScriptedFileLauncher leftLauncher, ScriptedFileLauncher rightLauncher, SettingsReadOutcome initialOutcome, out DualPaneSession panes)
    {
        PaneSession leftPane = new(_left,
            leftLauncher,
            PaneSessionConfiguration.Create(CommanderSessionTestValues.Capacity(), DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        PaneSession rightPane = new(_right,
            rightLauncher,
            PaneSessionConfiguration.Create(CommanderSessionTestValues.Capacity(), DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        panes = new DualPaneSession(leftPane, rightPane, _gateway);
        return new CommanderSession(
            panes,
            new SettingsSession(_store, initialOutcome, static _ => { }),
            new TransientScopeOwners(
                new AddressEditorSession(),
                new CommandPaletteSession(),
                new WindowAdjustmentSession(),
                new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog()),
                new KeyboardHelpSession()));
    }
}
