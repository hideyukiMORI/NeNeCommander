using System;
using System.Collections;
using System.Collections.Generic;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Owns a directory entry sequence without publishing a synchronization object or mutable storage.
/// Generic collection reads keep count and copy behavior for the existing directory and pane consumers.
/// </summary>
internal sealed record DirectoryEntrySnapshot : IReadOnlyList<DirectoryEntry>, ICollection<DirectoryEntry>
{
    private readonly List<DirectoryEntry> _entries;

    private DirectoryEntrySnapshot(IEnumerable<DirectoryEntry> entries)
    {
        _entries = [.. entries];
    }

    /// <summary>Gets the number of owned entries without enumeration.</summary>
    public int Count => _entries.Count;

    /// <summary>Gets one entry with the existing List index-boundary behavior.</summary>
    public DirectoryEntry this[int index] => _entries[index];

    bool ICollection<DirectoryEntry>.IsReadOnly => true;

    internal static DirectoryEntrySnapshot Create(IEnumerable<DirectoryEntry> entries)
    {
        return new DirectoryEntrySnapshot(entries);
    }

    /// <summary>Enumerates entries without exposing the owned list.</summary>
    public IEnumerator<DirectoryEntry> GetEnumerator()
    {
        return ((IEnumerable<DirectoryEntry>)_entries).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    bool ICollection<DirectoryEntry>.Contains(DirectoryEntry entry)
    {
        return _entries.Contains(entry);
    }

    void ICollection<DirectoryEntry>.CopyTo(DirectoryEntry[] array, int arrayIndex)
    {
        _entries.CopyTo(array, arrayIndex);
    }

    void ICollection<DirectoryEntry>.Add(DirectoryEntry entry)
    {
        throw new NotSupportedException();
    }

    void ICollection<DirectoryEntry>.Clear()
    {
        throw new NotSupportedException();
    }

    bool ICollection<DirectoryEntry>.Remove(DirectoryEntry entry)
    {
        throw new NotSupportedException();
    }
}
