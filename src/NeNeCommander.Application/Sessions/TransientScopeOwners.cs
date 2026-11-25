using System;

namespace NeNeCommander.Application.Sessions;

/// <summary>Carries the sole transient scope owners through explicit composition.</summary>
public sealed record TransientScopeOwners
{
    private readonly PaneScopeOwners _panes;

    /// <summary>Receives pane scopes and each complete application-scope owner.</summary>
    public TransientScopeOwners(
        PaneScopeOwners panes,
        CommandPaletteSession commandPalette,
        WindowAdjustmentSession windowAdjustment,
        KeyboardHelpSession keyboardHelp)
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

    /// <summary>Captures exactly one current state from each composed owner.</summary>
    internal TransientScopeSnapshot Capture()
    {
        return new TransientScopeSnapshot(_panes.Capture(), CommandPalette.Current,
            WindowAdjustment.Current, KeyboardHelp.Current);
    }

    /// <summary>Gets the sole AddressEditor scope owner.</summary>
    public AddressEditorSession AddressEditor => _panes.AddressEditor;

    /// <summary>Gets the sole CommandPalette scope owner.</summary>
    public CommandPaletteSession CommandPalette { get; }

    /// <summary>Gets the sole WindowAdjustment scope owner.</summary>
    public WindowAdjustmentSession WindowAdjustment { get; }

    /// <summary>Gets the sole Locations scope owner.</summary>
    public LocationsSession Locations => _panes.Locations;

    /// <summary>Gets the sole KeyboardHelp scope owner.</summary>
    public KeyboardHelpSession KeyboardHelp { get; }

    /// <summary>Gets the sole NameFilter scope owner.</summary>
    public PaneFilterSession NameFilter => _panes.NameFilter;
}
