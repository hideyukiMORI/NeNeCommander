using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Holds one pane's bounded successful-location sequence and current cursor. Construction remains
/// internal so <see cref="PaneReducer"/> is the only production owner of history transitions.
/// </summary>
internal sealed record PaneNavigationHistory
{
    internal const int LocationLimit = 100;

    private PaneNavigationHistory(ReadOnlyCollection<FileSystemPath> locations, int currentIndex)
    {
        Locations = locations;
        CurrentIndex = currentIndex;
    }

    /// <summary>Gets the complete bounded location sequence in visit order.</summary>
    internal IReadOnlyList<FileSystemPath> Locations { get; }

    /// <summary>Gets the index of the state location within <see cref="Locations"/>.</summary>
    internal int CurrentIndex { get; }

    /// <summary>Gets the location available to Back, or absence at the oldest retained location.</summary>
    internal FileSystemPath? BackTarget => CurrentIndex == 0 ? null : Locations[CurrentIndex - 1];

    /// <summary>Gets the location available to Forward, or absence at the newest retained location.</summary>
    internal FileSystemPath? ForwardTarget => CurrentIndex == Locations.Count - 1
        ? null
        : Locations[CurrentIndex + 1];

    /// <summary>Creates one validated immutable history snapshot.</summary>
    internal static PaneNavigationHistory Create(
        IReadOnlyList<FileSystemPath> locations,
        int currentIndex)
    {
        ArgumentNullException.ThrowIfNull(locations);
        if (locations.Count is 0 or > LocationLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(locations));
        }
        if (currentIndex < 0 || currentIndex >= locations.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(currentIndex));
        }

        List<FileSystemPath> owned = [];
        foreach (FileSystemPath location in locations)
        {
            ArgumentNullException.ThrowIfNull(location);
            owned.Add(location);
        }
        return new PaneNavigationHistory(owned.AsReadOnly(), currentIndex);
    }
}
