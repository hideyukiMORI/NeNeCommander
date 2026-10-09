using System;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Carries the facts one provider reported about a directory entry beyond its identity and kind
/// (ADR-0056): its closed visibility, its size, and its last-modification time. The adapter reports
/// them; Application reads them and never infers or fetches them. Size and time may each be unknown
/// independently; visibility is always reported. Grouping the facts keeps
/// <see cref="DirectoryEntry.Create"/> within the CS-013 parameter ceiling that ADR-0024 recorded.
/// </summary>
public sealed record EntryMetadata
{
    private EntryMetadata(EntryVisibility visibility, EntrySize size, EntryTimestamp modified)
    {
        Visibility = visibility;
        Size = size;
        Modified = modified;
    }

    /// <summary>
    /// Gets the closed visibility the provider reported for the entry. The listing carries the
    /// entry either way; only the pane transition decides whether the entry is shown.
    /// </summary>
    public EntryVisibility Visibility { get; }

    /// <summary>Gets the closed size the provider reported.</summary>
    public EntrySize Size { get; }

    /// <summary>Gets the closed last-modification time the provider reported.</summary>
    public EntryTimestamp Modified { get; }

    /// <summary>Creates metadata from closed values an adapter has already read at its boundary.</summary>
    /// <param name="visibility">Closed visibility of the entry.</param>
    /// <param name="size">Closed size of the entry.</param>
    /// <param name="modified">Closed last-modification time of the entry.</param>
    /// <returns>Complete immutable metadata.</returns>
    public static EntryMetadata Create(EntryVisibility visibility, EntrySize size, EntryTimestamp modified)
    {
        ArgumentNullException.ThrowIfNull(visibility);
        ArgumentNullException.ThrowIfNull(size);
        ArgumentNullException.ThrowIfNull(modified);
        return new EntryMetadata(visibility, size, modified);
    }

    /// <summary>
    /// Creates the metadata of an entry whose provider reported its visibility but neither size nor
    /// time.
    /// </summary>
    /// <param name="visibility">Closed visibility of the entry.</param>
    /// <returns>Metadata with an unknown size and an unknown time.</returns>
    public static EntryMetadata Unmeasured(EntryVisibility visibility)
    {
        return Create(visibility, EntrySize.Unknown, EntryTimestamp.Unknown);
    }
}
