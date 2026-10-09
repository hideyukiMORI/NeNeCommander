using System;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents the closed last-modification time one provider reports for a directory entry
/// (ADR-0056): either a known instant carried in UTC or unknown. A value the adapter could not read
/// stays unknown; no sentinel such as the minimum time stands for absence. Rendering in local time
/// is a Presentation decision.
/// </summary>
public abstract record EntryTimestamp
{
    private protected EntryTimestamp()
    {
    }

    /// <summary>Gets the time of an entry whose provider reports no modification time.</summary>
    public static EntryTimestamp Unknown { get; } = new UnknownEntryTimestamp();

    /// <summary>Creates the known modification time an adapter read at its boundary.</summary>
    /// <param name="utc">Instant the provider reported, with a zero offset.</param>
    /// <returns>A known timestamp holding exactly the given instant.</returns>
    /// <exception cref="ArgumentException">The offset is not zero, which is an adapter defect.</exception>
    public static EntryTimestamp Create(DateTimeOffset utc)
    {
        return utc.Offset == TimeSpan.Zero
            ? new KnownEntryTimestamp(utc)
            : throw new ArgumentException("The modification time must be carried in UTC.", nameof(utc));
    }

    private sealed record UnknownEntryTimestamp : EntryTimestamp;
}
