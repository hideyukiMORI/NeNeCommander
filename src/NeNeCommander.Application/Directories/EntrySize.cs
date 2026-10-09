using System;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents the closed size one provider reports for a directory entry (ADR-0056): either a
/// known non-negative byte count or unknown. A directory has no size, and a value the adapter could
/// not read stays unknown; no sentinel such as zero stands for absence.
/// </summary>
public abstract record EntrySize
{
    private protected EntrySize()
    {
    }

    /// <summary>Gets the size of an entry whose provider reports no byte count.</summary>
    public static EntrySize Unknown { get; } = new UnknownEntrySize();

    /// <summary>Creates the known size an adapter read at its boundary.</summary>
    /// <param name="bytes">Non-negative byte count the provider reported.</param>
    /// <returns>A known size holding exactly the given count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative, which is an adapter defect.</exception>
    public static EntrySize Create(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        return new KnownEntrySize(bytes);
    }

    private sealed record UnknownEntrySize : EntrySize;
}
