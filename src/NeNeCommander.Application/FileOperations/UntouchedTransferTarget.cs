namespace NeNeCommander.Application.FileOperations;

/// <summary>Reports a confirmed target not mutated by this operation, even when preparation artifacts exist.</summary>
public sealed record UntouchedTransferTarget : ProviderTargetObservation
{
    internal UntouchedTransferTarget(ObservedTransferEntry target)
        : base(target)
    {
    }

    private protected override void RestrictDerivation()
    {
    }
}
