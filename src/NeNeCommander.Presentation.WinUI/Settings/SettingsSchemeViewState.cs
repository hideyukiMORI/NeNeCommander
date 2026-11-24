using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Owns theme search and candidate preview without changing accepted or persisted settings.</summary>
public sealed class SettingsSchemeViewState
{
    private readonly RowSnapshot _allRows;
    private int _selectedIndex;

    private SettingsSchemeViewState(RowSnapshot rows, ColorScheme applied)
    {
        _allRows = rows;
        Rows = rows;
        CurrentRow = rows.Single(row => row.Scheme == applied);
        _ = Select(CurrentRow);
    }

    /// <summary>Gets the exact native query, reset by constructing a new open view.</summary>
    public string Query { get; private set; } = string.Empty;

    /// <summary>Gets the filtered, privately owned rows in catalog order.</summary>
    public IReadOnlyList<SettingsSchemeRow> Rows { get; private set; }

    /// <summary>Gets the applied theme independently of the filter.</summary>
    public SettingsSchemeRow CurrentRow { get; private set; }

    /// <summary>Gets the candidate being previewed, or absence for a query with no matches.</summary>
    public SettingsSchemeRow? SelectedRow => _selectedIndex < 0 ? null : Rows[_selectedIndex];

    /// <summary>Gets the temporary visual choice, restoring accepted settings when no candidate exists.</summary>
    public ColorScheme PreviewScheme => SelectedRow?.Scheme ?? CurrentRow.Scheme;

    /// <summary>Gets the full unfiltered catalog size.</summary>
    public int TotalCount => _allRows.Count;

    /// <summary>Gets the applied visible choice or the first result for entry through Tab.</summary>
    public SettingsSchemeRow? TabEntry => Rows.FirstOrDefault(row => row.Scheme == CurrentRow.Scheme) ?? (Rows.Count == 0 ? null : Rows[0]);

    /// <summary>Creates an open view from the current settings projection and existing resource resolver.</summary>
    public static SettingsSchemeViewState Create(SettingsPresentation presentation, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(localize);
        RowSnapshot rows = RowSnapshot.Create(presentation.Schemes.Select(option => SettingsSchemeRow.Create(option, localize)));
        ColorScheme applied = presentation.Schemes.Single(static option => option.IsSelected).Scheme;
        return new SettingsSchemeViewState(rows, applied);
    }

    /// <summary>Filters names and identifiers without changing the applied theme or its saved settings.</summary>
    public void UpdateQuery(string query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.Equals(Query, query, StringComparison.Ordinal))
        {
            return;
        }
        Query = query;
        Rows = query.Length == 0 ? _allRows : RowSnapshot.Create(_allRows.Where(row =>
            row.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            row.Scheme.Identifier.Contains(query, StringComparison.OrdinalIgnoreCase)));
        _selectedIndex = Rows.Count == 0 ? -1 : 0;
    }

    /// <summary>Moves the preview toward the next candidate without wrapping or saving.</summary>
    public void MoveNext()
    {
        if (_selectedIndex >= 0 && _selectedIndex < Rows.Count - 1)
        {
            _selectedIndex++;
        }
    }

    /// <summary>Moves the preview toward the previous candidate without wrapping or saving.</summary>
    public void MovePrevious()
    {
        if (_selectedIndex > 0)
        {
            _selectedIndex--;
        }
    }

    /// <summary>Selects an owned visible row for preview, refusing stale or foreign row objects.</summary>
    public bool Select(SettingsSchemeRow row)
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

    /// <summary>Observes a session-owned change while keeping query, row identities and filtered storage.</summary>
    public void ObserveApplied(ColorScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);
        CurrentRow = _allRows.Single(row => row.Scheme == scheme);
    }

    private sealed record RowSnapshot : IReadOnlyList<SettingsSchemeRow>
    {
        private readonly SettingsSchemeRow[] _rows;

        private RowSnapshot(IEnumerable<SettingsSchemeRow> rows)
        {
            _rows = [.. rows];
        }

        public int Count => _rows.Length;

        public SettingsSchemeRow this[int index] => _rows[index];

        internal static RowSnapshot Create(IEnumerable<SettingsSchemeRow> rows)
        {
            return new RowSnapshot(rows);
        }

        public IEnumerator<SettingsSchemeRow> GetEnumerator()
        {
            return ((IEnumerable<SettingsSchemeRow>)_rows).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
