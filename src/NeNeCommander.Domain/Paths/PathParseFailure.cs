namespace NeNeCommander.Domain.Paths;

/// <summary>
/// Represents a rejected filesystem path and its closed failure kind.
/// </summary>
public sealed record PathParseFailure : PathParseOutcome
{
    internal PathParseFailure(PathParseFailureKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the reason the input was rejected.</summary>
    public PathParseFailureKind Kind { get; }
}
