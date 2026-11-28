namespace NeNeCommander.Application.FileOperations;

/// <summary>Reports full original-content readback after possible target mutation; it is not an automatic rollback claim.</summary>
public sealed record OriginalTargetContentVerified : ProviderTargetObservation
{
    internal OriginalTargetContentVerified(ObservedTransferEntry target, FileContentEvidence content)
        : base(target)
    {
        Content = content;
    }

    /// <summary>Gets the complete observed content evidence.</summary>
    public FileContentEvidence Content { get; }

    private protected override void RestrictDerivation()
    {
    }
}
