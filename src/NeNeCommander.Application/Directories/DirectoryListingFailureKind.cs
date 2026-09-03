namespace NeNeCommander.Application.Directories;

/// <summary>
/// Identifies one closed reason an adapter-supplied listing was rejected.
/// </summary>
public abstract record DirectoryListingFailureKind
{
    /// <summary>Gets the failure for a null entry element.</summary>
    public static DirectoryListingFailureKind NullEntry { get; } = new NullEntryFailure();

    /// <summary>Gets the failure for two entries sharing one provider identity.</summary>
    public static DirectoryListingFailureKind DuplicateEntry { get; } = new DuplicateEntryFailure();

    /// <summary>Gets the failure for an entry count above the fixed listing boundary.</summary>
    public static DirectoryListingFailureKind TooManyEntries { get; } = new TooManyEntriesFailure();

    private DirectoryListingFailureKind()
    {
    }

    private sealed record NullEntryFailure : DirectoryListingFailureKind;
    private sealed record DuplicateEntryFailure : DirectoryListingFailureKind;
    private sealed record TooManyEntriesFailure : DirectoryListingFailureKind;
}
