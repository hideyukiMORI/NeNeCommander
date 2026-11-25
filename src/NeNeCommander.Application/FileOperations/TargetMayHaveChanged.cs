namespace NeNeCommander.Application.FileOperations;

/// <summary>Reports a target that may contain partial or truncated content; an exception does not imply no effect.</summary>
public sealed record TargetMayHaveChanged : ProviderTargetObservation
{
    internal TargetMayHaveChanged(ObservedTransferEntry target)
        : base(target)
    {
    }

    private protected override void RestrictDerivation()
    {
    }
}
