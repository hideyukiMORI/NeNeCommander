namespace NeNeCommander.Application.FileOperations;

/// <summary>Identifies one explicit decision for a transfer collision.</summary>
public abstract record TransferConflictDecision
{
    /// <summary>Leaves the source untransferred.</summary>
    public static TransferConflictDecision Skip { get; } = new SkipDecision();

    /// <summary>Transfers to the conflict's provider-allocated alternate target.</summary>
    public static TransferConflictDecision KeepBoth { get; } = new KeepBothDecision();

    /// <summary>Cancels the whole operation without starting another effect.</summary>
    public static TransferConflictDecision Cancel { get; } = new CancelDecision();

    private TransferConflictDecision()
    {
    }

    private sealed record SkipDecision : TransferConflictDecision;
    private sealed record KeepBothDecision : TransferConflictDecision;
    private sealed record CancelDecision : TransferConflictDecision;
}
