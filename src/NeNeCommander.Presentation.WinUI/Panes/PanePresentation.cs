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
    internal PanePresentation(
        PaneRows rows,
        PaneRow? focusRow,
        PaneStatus status,
        string addressText,
        PaneSnapshot sourceSnapshot,
        PaneFrame sourceFrame)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(addressText);
        ArgumentNullException.ThrowIfNull(sourceSnapshot);
        ArgumentNullException.ThrowIfNull(sourceFrame);
        OwnedRows = rows;
        Rows = rows.View;
        FocusRow = focusRow;
        Status = status;
        AddressText = addressText;
        SourceSnapshot = sourceSnapshot;
        SourceFrame = sourceFrame;
    }

    internal PaneRows OwnedRows { get; }

    internal PaneSnapshot SourceSnapshot { get; }

    internal PaneFrame SourceFrame { get; }

    /// <summary>Gets the ordered rows to display with their selection marks, or an empty list when nothing is listed.</summary>
    public IReadOnlyList<PaneRow> Rows { get; }

    /// <summary>Gets the row that holds focus, or absence when there is no focus item.</summary>
    public PaneRow? FocusRow { get; }

    /// <summary>Gets the status the pane shows.</summary>
    public PaneStatus Status { get; }

    /// <summary>Gets the canonical text of the listed or targeted location, or empty text.</summary>
    public string AddressText { get; }
}
