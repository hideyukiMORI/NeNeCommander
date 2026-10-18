namespace NeNeCommander.Application.Directories;

/// <summary>
/// Identifies whether a listing contains every direct entry or stopped at its entry boundary.
/// </summary>
public abstract record DirectoryListingCompleteness
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the state in which every representable direct entry was read.</summary>
    public static DirectoryListingCompleteness Complete { get; } = new CompleteState();

    /// <summary>Gets the state in which enumeration stopped at the requested entry boundary.</summary>
    public static DirectoryListingCompleteness Bounded { get; } = new BoundedState();

    private DirectoryListingCompleteness()
    {
    }

    private sealed record CompleteState : DirectoryListingCompleteness
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record BoundedState : DirectoryListingCompleteness
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
