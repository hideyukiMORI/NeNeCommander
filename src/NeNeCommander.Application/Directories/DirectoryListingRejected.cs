namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents a directory listing rejected before it could reach a pane.
/// </summary>
public sealed record DirectoryListingRejected : DirectoryListingCreation
{
    internal DirectoryListingRejected(DirectoryListingFailureKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the closed rejection reason.</summary>
    public DirectoryListingFailureKind Kind { get; }
}
