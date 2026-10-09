using System;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Carries the provider-reported size and last-modification time of one directory entry
/// (ADR-0056). The adapter reports both facts; Application reads them and never infers or fetches
/// them. Either fact may be unknown independently of the other.
/// </summary>
public sealed record EntryMetadata
{
    private EntryMetadata(EntrySize size, EntryTimestamp modified)
    {
        Size = size;
        Modified = modified;
    }

    /// <summary>Gets the metadata of an entry whose provider reports neither size nor time.</summary>
    public static EntryMetadata Unknown { get; } = new(EntrySize.Unknown, EntryTimestamp.Unknown);

    /// <summary>Gets the closed size the provider reported.</summary>
    public EntrySize Size { get; }

    /// <summary>Gets the closed last-modification time the provider reported.</summary>
    public EntryTimestamp Modified { get; }

    /// <summary>Creates metadata from closed values an adapter has already read at its boundary.</summary>
    /// <param name="size">Closed size of the entry.</param>
    /// <param name="modified">Closed last-modification time of the entry.</param>
    /// <returns>Complete immutable metadata.</returns>
    public static EntryMetadata Create(EntrySize size, EntryTimestamp modified)
    {
        ArgumentNullException.ThrowIfNull(size);
        ArgumentNullException.ThrowIfNull(modified);
        return new EntryMetadata(size, modified);
    }
}
