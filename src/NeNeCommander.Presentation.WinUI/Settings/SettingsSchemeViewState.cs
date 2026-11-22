using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Owns transient theme search and observes applied settings without selecting or persisting them.</summary>
public sealed class SettingsSchemeViewState
{
    private readonly RowSnapshot _allRows;

    private SettingsSchemeViewState(RowSnapshot rows, ColorScheme applied)
    {
        _allRows = rows;
        Rows = rows;
        CurrentRow = rows.Single(row => row.Scheme == applied);
    }

    /// <summary>Gets the exact native query, reset by constructing a new open view.</summary>
    public string Query { get; private set; } = string.Empty;

    /// <summary>Gets the filtered, privately owned rows in catalog order.</summary>
    public IReadOnlyList<SettingsSchemeRow> Rows { get; private set; }

    /// <summary>Gets the applied theme independently of the filter.</summary>
    public SettingsSchemeRow CurrentRow { get; private set; }

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
