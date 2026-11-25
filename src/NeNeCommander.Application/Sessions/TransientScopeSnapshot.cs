using System;

namespace NeNeCommander.Application.Sessions;

/// <summary>Carries the sole transient scope states through explicit composition.</summary>
public sealed record TransientScopeSnapshot
{
    private readonly PaneScopeSnapshot _panes;

    /// <summary>Receives pane scopes and each complete application-scope state.</summary>
    internal TransientScopeSnapshot(
        PaneScopeSnapshot panes,
        CommandPaletteState commandPalette,
        WindowAdjustmentState windowAdjustment,
        KeyboardHelpState keyboardHelp)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(commandPalette);
        ArgumentNullException.ThrowIfNull(windowAdjustment);
        ArgumentNullException.ThrowIfNull(keyboardHelp);
        _panes = panes;
        CommandPalette = commandPalette;
        WindowAdjustment = windowAdjustment;
        KeyboardHelp = keyboardHelp;
    }

    /// <summary>Gets the sole AddressEditor scope state.</summary>
    public AddressEditorState AddressEditor => _panes.AddressEditor;

    /// <summary>Gets the sole CommandPalette scope state.</summary>
    public CommandPaletteState CommandPalette { get; }

    /// <summary>Gets the sole WindowAdjustment scope state.</summary>
    public WindowAdjustmentState WindowAdjustment { get; }

    /// <summary>Gets the sole Locations scope state.</summary>
    public LocationsState Locations => _panes.Locations;

    /// <summary>Gets the sole KeyboardHelp scope state.</summary>
    public KeyboardHelpState KeyboardHelp { get; }

    /// <summary>Gets the sole NameFilter scope state.</summary>
    public PaneFilterState NameFilter => _panes.NameFilter;
}
