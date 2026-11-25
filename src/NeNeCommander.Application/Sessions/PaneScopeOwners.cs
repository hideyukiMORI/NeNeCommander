using System;

namespace NeNeCommander.Application.Sessions;

/// <summary>Groups the explicit owners of pane-specific transient input without a second registry.</summary>
public sealed record PaneScopeOwners
{
    /// <summary>Receives the three owners created by the composition root.</summary>
    public PaneScopeOwners(AddressEditorSession addressEditor, LocationsSession locations, PaneFilterSession nameFilter)
    {
        ArgumentNullException.ThrowIfNull(addressEditor);
        ArgumentNullException.ThrowIfNull(locations);
        ArgumentNullException.ThrowIfNull(nameFilter);
        AddressEditor = addressEditor;
        Locations = locations;
        NameFilter = nameFilter;
    }

    internal AddressEditorSession AddressEditor { get; }

    internal LocationsSession Locations { get; }

    internal PaneFilterSession NameFilter { get; }

    internal PaneScopeSnapshot Capture()
    {
        return new PaneScopeSnapshot(AddressEditor.Current, Locations.Current, NameFilter.Current);
    }
}
