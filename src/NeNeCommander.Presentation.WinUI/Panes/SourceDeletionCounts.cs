using System;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Separates fully removed transfer sources from sources with confirmed partial removal.</summary>
public sealed record SourceDeletionCounts
{
    private SourceDeletionCounts(int deleted, int partiallyDeleted)
    {
        Deleted = deleted;
        PartiallyDeleted = partiallyDeleted;
    }

    /// <summary>Gets the number of entirely removed sources.</summary>
    public int Deleted { get; }
    /// <summary>Gets the number of sources with removed descendants and remaining entries.</summary>
    public int PartiallyDeleted { get; }

    /// <summary>Creates nonnegative, independently reported deletion counts.</summary>
    public static SourceDeletionCounts Create(int deleted, int partiallyDeleted)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(deleted);
        ArgumentOutOfRangeException.ThrowIfNegative(partiallyDeleted);
        return new SourceDeletionCounts(deleted, partiallyDeleted);
    }
}
