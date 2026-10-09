using System;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Carries the owners of every transient scope the application session coordinates. It is the one
/// place a later transient scope is added, so the session constructor keeps its arity.
/// </summary>
public sealed record TransientScopeOwners
{
    /// <summary>Initializes the set of transient scope owners the composition root created.</summary>
    /// <param name="addressEditor">Sole address editor scope owner.</param>
    /// <param name="commandPalette">Sole command palette scope owner.</param>
    /// <param name="windowAdjustment">Sole window adjustment scope owner.</param>
    /// <param name="locations">Sole Locations picker scope owner.</param>
    public TransientScopeOwners(
        AddressEditorSession addressEditor,
        CommandPaletteSession commandPalette,
        WindowAdjustmentSession windowAdjustment,
        LocationsSession locations)
    {
        ArgumentNullException.ThrowIfNull(addressEditor);
        ArgumentNullException.ThrowIfNull(commandPalette);
        ArgumentNullException.ThrowIfNull(windowAdjustment);
        ArgumentNullException.ThrowIfNull(locations);
        AddressEditor = addressEditor;
        CommandPalette = commandPalette;
        WindowAdjustment = windowAdjustment;
        Locations = locations;
    }

    /// <summary>Gets the sole address editor scope owner.</summary>
    public AddressEditorSession AddressEditor { get; }

    /// <summary>Gets the sole command palette scope owner.</summary>
    public CommandPaletteSession CommandPalette { get; }

    /// <summary>Gets the sole window adjustment scope owner.</summary>
    public WindowAdjustmentSession WindowAdjustment { get; }

    /// <summary>Gets the sole Locations picker scope owner.</summary>
    public LocationsSession Locations { get; }
}
