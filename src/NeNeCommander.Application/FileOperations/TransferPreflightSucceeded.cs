using System.Collections.Generic;
using System.Collections.Immutable;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Contains the complete exact transfer plan.</summary>
public sealed record TransferPreflightSucceeded : TransferPreflightOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal TransferPreflightSucceeded(ImmutableList<TransferPlanEntry> plan)
    {
        Plan = plan;
    }

    /// <summary>Gets the ordered plan for every source.</summary>
    public IReadOnlyList<TransferPlanEntry> Plan { get; }
    /// <inheritdoc />
    public override FileOperationFailureKind? Failure => null;
}
