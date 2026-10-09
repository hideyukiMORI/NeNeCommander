using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Owns one stable observable row source and its provider-aware path index so a projection can
/// replace only rows whose render-ready values changed. It also keeps the metadata format its rows
/// were projected with, because rows formatted with another format cannot be reused.
/// </summary>
internal sealed class PaneRows
{
    private readonly Dictionary<FileSystemPath, int> _indexes;
    private readonly ObservableCollection<PaneRow> _rows;

    internal PaneRows(IReadOnlyList<PaneRow> rows, EntryMetadataFormat format)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(format);
        Format = format;
        _indexes = new Dictionary<FileSystemPath, int>(FileSystemPathIdentityComparer.Instance);
        _rows = new ObservableCollection<PaneRow>(rows);
        for (int index = 0; index < rows.Count; index++)
        {
            PaneRow row = rows[index];
            _indexes.Add(row.Entry.Path, index);
        }
        View = new ReadOnlyObservableCollection<PaneRow>(_rows);
    }

    internal PaneRow this[int index] => _rows[index];

    internal IReadOnlyList<PaneRow> View { get; }

    internal EntryMetadataFormat Format { get; }

    internal bool TryGetIndex(FileSystemPath path, out int index)
    {
        return _indexes.TryGetValue(path, out index);
    }

    internal void Replace(int index, PaneRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        _rows[index] = row;
    }
}
