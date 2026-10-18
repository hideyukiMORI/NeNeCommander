namespace NeNeCommander.Application.Directories;

/// <summary>Represents a byte count the provider reported for one entry; it is never negative.</summary>
public sealed record KnownEntrySize : EntrySize
{
    private protected override void RestrictDerivation()
    {
    }

    internal KnownEntrySize(long bytes)
    {
        Bytes = bytes;
    }

    /// <summary>Gets the non-negative byte count the provider reported.</summary>
    public long Bytes { get; }
}
