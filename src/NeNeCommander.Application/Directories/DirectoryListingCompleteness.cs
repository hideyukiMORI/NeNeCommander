namespace NeNeCommander.Application.Directories;

/// <summary>
/// Identifies whether a listing contains every direct entry or stopped at its entry boundary.
/// </summary>
public abstract record DirectoryListingCompleteness
{
    /// <summary>Gets the state in which every representable direct entry was read.</summary>
    public static DirectoryListingCompleteness Complete { get; } = new CompleteState();

    /// <summary>Gets the state in which enumeration stopped at the requested entry boundary.</summary>
    public static DirectoryListingCompleteness Bounded { get; } = new BoundedState();

    private DirectoryListingCompleteness()
    {
    }

    private sealed record CompleteState : DirectoryListingCompleteness;
    private sealed record BoundedState : DirectoryListingCompleteness;
}
