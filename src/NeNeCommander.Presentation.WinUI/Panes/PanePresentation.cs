using System;
using System.Collections.Generic;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents the render-ready projection of one pane snapshot. The host assigns these values
/// to framework controls without making further decisions.
/// </summary>
public sealed record PanePresentation
{
    internal PanePresentation(
        IReadOnlyList<PaneRow> rows,
        PaneRow? focusRow,
        PaneStatus status,
        string addressText)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(addressText);
        Rows = rows;
        FocusRow = focusRow;
        Status = status;
        AddressText = addressText;
    }

    /// <summary>Gets the ordered rows to display with their selection marks, or an empty list when nothing is listed.</summary>
    public IReadOnlyList<PaneRow> Rows { get; }

    /// <summary>Gets the row that holds focus, or absence when there is no focus item.</summary>
    public PaneRow? FocusRow { get; }

    /// <summary>Gets the status the pane shows.</summary>
    public PaneStatus Status { get; }

    /// <summary>Gets the canonical text of the listed or targeted location, or empty text.</summary>
    public string AddressText { get; }
}
