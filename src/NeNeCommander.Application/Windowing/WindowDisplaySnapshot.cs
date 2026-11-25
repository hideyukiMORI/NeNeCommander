using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NeNeCommander.Application.Windowing;

/// <summary>Owns a complete fresh display sequence without leaking mutable storage or a synchronization root.</summary>
public sealed record WindowDisplaySnapshot : IReadOnlyList<WindowDisplay>
{
    private readonly List<WindowDisplay> _displays;

    private WindowDisplaySnapshot(WindowDisplay current, List<WindowDisplay> displays)
    {
        Current = current;
        _displays = displays;
    }

    /// <summary>Gets the exact facts for the SDK-selected current display.</summary>
    public WindowDisplay Current { get; }

    /// <summary>Gets the count of distinct observed identities.</summary>
    public int Count => _displays.Count;

    /// <summary>Gets an observation in opaque-identity order, not display-cycle order.</summary>
    /// <param name="index">Zero-based observation index.</param>
    /// <returns>The owned immutable observation.</returns>
    public WindowDisplay this[int index] => _displays[index];

    /// <summary>Copies and validates observations, including the complete current-display facts.</summary>
    /// <param name="current">Current display that must occur in the supplied observations.</param>
    /// <param name="displays">Fresh complete sequence with unique identities and no absent entries.</param>
    /// <returns>An owned snapshot, stable even if the caller changes its list.</returns>
    public static WindowDisplaySnapshot Create(WindowDisplay current, IReadOnlyList<WindowDisplay> displays)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(displays);
        List<WindowDisplay> owned = [.. displays];
        foreach (WindowDisplay display in owned)
        {
            ArgumentNullException.ThrowIfNull(display);
        }
        if (!owned.Contains(current) || owned.Select(display => display.Identity).Distinct().Count() != owned.Count)
        {
            throw new ArgumentException("Display observations must have unique identities and include the current display.", nameof(displays));
        }
        owned.Sort((left, right) => left.Identity.Value.CompareTo(right.Identity.Value));
        return new WindowDisplaySnapshot(current, owned);
    }

    internal bool MatchesPlacement(WindowPlacement placement)
    {
        HashSet<WindowBounds> areas = [.. placement.WorkAreas.Attached];
        return placement.WorkAreas.Current == Current.WorkArea &&
            areas.SetEquals(_displays.Select(display => display.WorkArea));
    }
    /// <summary>Enumerates immutable observations without exposing the owned list.</summary>
    /// <returns>The observation enumerator.</returns>
    public IEnumerator<WindowDisplay> GetEnumerator()
    {
        return _displays.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
