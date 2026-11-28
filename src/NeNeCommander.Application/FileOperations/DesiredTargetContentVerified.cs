namespace NeNeCommander.Application.FileOperations;

/// <summary>Reports full desired-content verification independently of later cleanup or source deletion.</summary>
public sealed record DesiredTargetContentVerified : ProviderTargetObservation
{
    internal DesiredTargetContentVerified(ObservedTransferEntry target, FileContentEvidence content)
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
