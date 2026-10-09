using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Locations;

/// <summary>
/// Represents everything the Locations picker renders for one Application state: whether it is
/// shown, its localized status and per-section texts, its rows, the focused row, and its key hints.
/// A section failure is carried by its own text and never by the absence of rows alone.
/// </summary>
public sealed record LocationsPresentation
{
    internal LocationsPresentation(
        LocationsState sourceState,
        LocationsPresentationTexts texts,
        IReadOnlyList<LocationRow> rows,
        IReadOnlyList<KeyHint> keyHints)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(texts);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(keyHints);
        SourceState = sourceState;
        Texts = texts;
        Rows = rows;
        KeyHints = keyHints;
        FocusRow = sourceState is LocationsOpen { FocusItem: { } focus }
            ? rows.First(row => ReferenceEquals(row.Item, focus))
            : null;
    }

    /// <summary>Gets the exact Application state this presentation projects.</summary>
    public LocationsState SourceState { get; }

    /// <summary>Gets whether the picker is shown, which is every state except closed.</summary>
    public bool IsShown => SourceState is not LocationsClosed;

    /// <summary>Gets the localized status and per-section texts; each is empty when it has nothing to say.</summary>
    public LocationsPresentationTexts Texts { get; }

    /// <summary>Gets the rows of both sections, drives first.</summary>
    public IReadOnlyList<LocationRow> Rows { get; }

    /// <summary>Gets the row of the Application focus item, or absence when no entry is listed.</summary>
    public LocationRow? FocusRow { get; }

    /// <summary>Gets the hints generated from the picker's declared key bindings.</summary>
    public IReadOnlyList<KeyHint> KeyHints { get; }
}
