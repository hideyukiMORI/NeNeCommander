using System;
using System.Collections.Generic;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents the render-ready projection of one pane snapshot. The host assigns these values
/// to framework controls without making further decisions.
/// </summary>
public sealed record PanePresentation
{
    private readonly PaneProjectionSource _source;
    private readonly PaneHeaderPresentation _header;

    internal PanePresentation(
        PaneRows rows,
        PaneRow? focusRow,
        PaneProjectionSource source,
        PaneHeaderPresentation header)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(header);
        OwnedRows = rows;
        Rows = rows.View;
        FocusRow = focusRow;
        _source = source;
        _header = header;
    }

    internal PaneRows OwnedRows { get; }

    internal PaneSnapshot SourceSnapshot => _source.Snapshot;

    internal PaneFrame SourceFrame => _source.Frame;

    /// <summary>Gets the ordered rows to display with their selection marks, or an empty list when nothing is listed.</summary>
    public IReadOnlyList<PaneRow> Rows { get; }

    /// <summary>Gets the row that holds focus, or absence when there is no focus item.</summary>
    public PaneRow? FocusRow { get; }

    /// <summary>Gets the status the pane shows.</summary>
    public PaneStatus Status => _header.Status;

    /// <summary>Gets the canonical text of the listed or targeted location, or empty text.</summary>
    public string AddressText => _header.AddressText;

    /// <summary>
    /// Gets the sort order the listed rows follow, or absence when nothing is listed and therefore
    /// nothing is ordered.
    /// </summary>
    public PaneSortStatus? SortStatus => _header.SortStatus;

    /// <summary>Gets an indication of the listed location, or absence when no indication applies.</summary>
    public PaneLocationStatus? LocationStatus => _header.LocationStatus;
}
