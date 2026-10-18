namespace NeNeCommander.Domain.Paths;

/// <summary>
/// Represents a rejected filesystem path and its closed failure kind.
/// </summary>
public sealed record PathParseFailure : PathParseOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal PathParseFailure(PathParseFailureKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the reason the input was rejected.</summary>
    public PathParseFailureKind Kind { get; }
}
