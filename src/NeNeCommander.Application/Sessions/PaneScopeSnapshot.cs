using System;

namespace NeNeCommander.Application.Sessions;

/// <summary>Carries one captured state per pane-specific scope, privately behind the complete snapshot.</summary>
internal sealed record PaneScopeSnapshot
{
    internal PaneScopeSnapshot(AddressEditorState addressEditor, LocationsState locations, PaneFilterState nameFilter)
    {
        ArgumentNullException.ThrowIfNull(addressEditor);
        ArgumentNullException.ThrowIfNull(locations);
        ArgumentNullException.ThrowIfNull(nameFilter);
        AddressEditor = addressEditor;
        Locations = locations;
        NameFilter = nameFilter;
    }

    internal AddressEditorState AddressEditor { get; }

    internal LocationsState Locations { get; }

    internal PaneFilterState NameFilter { get; }
}
