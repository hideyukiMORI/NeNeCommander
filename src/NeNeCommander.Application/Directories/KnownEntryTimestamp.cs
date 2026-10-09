using System;

namespace NeNeCommander.Application.Directories;

/// <summary>Represents a modification time the provider reported for one entry, carried in UTC.</summary>
public sealed record KnownEntryTimestamp : EntryTimestamp
{
    internal KnownEntryTimestamp(DateTimeOffset utc)
    {
        Utc = utc;
    }

    /// <summary>Gets the reported instant; its offset is always zero.</summary>
    public DateTimeOffset Utc { get; }
}
