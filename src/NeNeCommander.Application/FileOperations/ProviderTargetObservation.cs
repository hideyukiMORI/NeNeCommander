using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Reports one confirmed existing target's content state without promising rollback or atomic replacement.</summary>
public abstract record ProviderTargetObservation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected ProviderTargetObservation(ObservedTransferEntry target)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
    }

    /// <summary>Gets the exact observed target object and qualified path, not a new pathname occupant.</summary>
    public ObservedTransferEntry Target { get; }

    /// <summary>Reports that this operation has not entered a target-mutating call.</summary>
    public static ProviderTargetObservation Untouched(ObservedTransferEntry target)
    {
        return new UntouchedTransferTarget(target);
    }

    /// <summary>Reports uncertainty entered before the first potentially mutating write or truncate.</summary>
    public static ProviderTargetObservation MayHaveChanged(ObservedTransferEntry target)
    {
        return new TargetMayHaveChanged(target);
    }

    /// <summary>Reports exact original bytes proved by complete readback after possible mutation.</summary>
    public static ProviderTargetObservation OriginalVerified(ObservedTransferEntry target, FileContentEvidence content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new OriginalTargetContentVerified(target, content);
    }

    /// <summary>Reports complete desired bytes proved at the exact target; cleanup may still fail.</summary>
    public static ProviderTargetObservation DesiredVerified(ObservedTransferEntry target, FileContentEvidence content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new DesiredTargetContentVerified(target, content);
    }
}
