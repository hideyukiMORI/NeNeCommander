using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Owns one open palette's native query projection and selected filtered row.</summary>
public sealed class CommandPaletteViewState
{
    private readonly IReadOnlyList<CommandPaletteRow> _allRows;
    private int _selectedIndex;

    internal CommandPaletteViewState(
        CommandPaletteOpen sourceState,
        IReadOnlyList<CommandPaletteRow> rows)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(rows);
        SourceState = sourceState;
        _allRows = rows;
        Rows = rows;
        _selectedIndex = rows.Count == 0 ? -1 : 0;
        Query = string.Empty;
    }

    /// <summary>Gets the exact Application open state qualified by this view state.</summary>
    public CommandPaletteOpen SourceState { get; }

    /// <summary>Gets the exact native query last projected.</summary>
    public string Query { get; private set; }

    /// <summary>Gets the filtered rows in stable catalog order.</summary>
    public IReadOnlyList<CommandPaletteRow> Rows { get; private set; }

    /// <summary>Gets the localized target summary shared by every captured row.</summary>
    public string Target => _allRows[0].Target;

    /// <summary>Gets the localized opposite-pane summary shared by every captured row.</summary>
    public string Opposite => _allRows[0].Opposite;

    /// <summary>Gets the selected filtered row, or absence when the query has zero results.</summary>
    public CommandPaletteRow? SelectedRow => _selectedIndex < 0 ? null : Rows[_selectedIndex];

    /// <summary>Projects a changed native query and selects the first result.</summary>
    public void UpdateQuery(string query)
    {
        ArgumentNullException.ThrowIfNull(query);
        Query = query;
        List<CommandPaletteRow> filtered = [.. _allRows.Where(row =>
            row.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            row.Shortcut.Contains(query, StringComparison.OrdinalIgnoreCase))];
        Rows = new ReadOnlyCollection<CommandPaletteRow>(filtered);
        _selectedIndex = filtered.Count == 0 ? -1 : 0;
    }

    /// <summary>Moves selection toward the following row without wrapping.</summary>
    public void MoveNext()
    {
        if (_selectedIndex >= 0 && _selectedIndex < Rows.Count - 1)
        {
            _selectedIndex++;
        }
    }

    /// <summary>Moves selection toward the preceding row without wrapping.</summary>
    public void MovePrevious()
    {
        if (_selectedIndex > 0)
        {
            _selectedIndex--;
        }
    }

    /// <summary>Selects one row from the current filtered projection.</summary>
    /// <returns><see langword="true"/> when this view owns the row; otherwise false.</returns>
    public bool Select(CommandPaletteRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        for (int index = 0; index < Rows.Count; index++)
        {
            if (ReferenceEquals(Rows[index], row))
            {
                _selectedIndex = index;
                return true;
            }
        }
        return false;
    }
}
