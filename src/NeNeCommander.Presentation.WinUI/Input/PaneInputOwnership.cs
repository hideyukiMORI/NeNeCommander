using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Projects whether ordinary pane input is free of another declared owner.</summary>
internal static class PaneInputOwnership
{
    internal static bool IsAvailable(CommanderSnapshot snapshot)
    {
        return snapshot.Settings.Editor == SettingsEditorState.Closed &&
            snapshot.Scopes.AddressEditor is AddressEditorClosed &&
            snapshot.Scopes.CommandPalette is not CommandPaletteOpen &&
            snapshot.Scopes.KeyboardHelp is not KeyboardHelpOpen &&
            snapshot.Scopes.WindowAdjustment is not WindowAdjustmentOpen &&
            snapshot.Scopes.Locations is LocationsClosed &&
            snapshot.Panes.Operation is not (OperationRunning or OperationAwaitingConfirmation or OperationAwaitingName or OperationAwaitingConflict) &&
            snapshot.Panes.Left.Activity is not (PaneLoading or PaneLaunching) &&
            snapshot.Panes.Right.Activity is not (PaneLoading or PaneLaunching);
    }
}
