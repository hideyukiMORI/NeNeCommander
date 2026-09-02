using System;

namespace NeNeCommander.Infrastructure.Windows.Paths;

/// <summary>Represents a candidate rejected from an operation root.</summary>
public sealed record RejectedPathContainment : PathContainmentOutcome
{
    internal RejectedPathContainment(PathContainmentFailureKind kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        Kind = kind;
    }

    /// <summary>Gets the closed containment failure.</summary>
    public PathContainmentFailureKind Kind { get; }
}
