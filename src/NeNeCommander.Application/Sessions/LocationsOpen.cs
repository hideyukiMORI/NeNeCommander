using System;
using System.Collections.Generic;
using NeNeCommander.Application.Locations;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the open Locations picker: two independently decided sections, the listed entries of
/// both in order (drives first, then WSL distribution roots), and one focus index over them.
/// </summary>
public sealed record LocationsOpen : LocationsState
{
    internal LocationsOpen(
        PaneSide activeSide,
        DriveSection drives,
        WslRootSection wslRoots,
        int focusIndex)
    {
        ArgumentNullException.ThrowIfNull(activeSide);
        ArgumentNullException.ThrowIfNull(drives);
        ArgumentNullException.ThrowIfNull(wslRoots);
        List<LocationItem> items = [];
        if (drives is DriveSectionListed listedDrives)
        {
            items.AddRange(listedDrives.Items);
        }
        if (wslRoots is WslRootSectionListed listedRoots)
        {
            items.AddRange(listedRoots.Items);
        }
        ActiveSide = activeSide;
        Drives = drives;
        WslRoots = wslRoots;
        Items = items.AsReadOnly();
        FocusIndex = focusIndex;
    }

    /// <summary>Gets the pane that was active at open and that a selection navigates.</summary>
    public PaneSide ActiveSide { get; }

    /// <summary>Gets the drives section.</summary>
    public DriveSection Drives { get; }

    /// <summary>Gets the WSL distribution roots section.</summary>
    public WslRootSection WslRoots { get; }

    /// <summary>Gets every listed entry of both sections, drives first.</summary>
    public IReadOnlyList<LocationItem> Items { get; }

    /// <summary>Gets the focused entry, or absence when neither section lists an entry.</summary>
    public LocationItem? FocusItem => FocusIndex < Items.Count ? Items[FocusIndex] : null;

    internal int FocusIndex { get; }

    /// <summary>
    /// Moves the focus by one step. The focus stops at the first and last entries; a step past
    /// either boundary keeps this exact state.
    /// </summary>
    internal LocationsOpen MoveFocus(int step)
    {
        int target = FocusIndex + step;
        return target < 0 || target >= Items.Count
            ? this
            : new LocationsOpen(ActiveSide, Drives, WslRoots, target);
    }
}
