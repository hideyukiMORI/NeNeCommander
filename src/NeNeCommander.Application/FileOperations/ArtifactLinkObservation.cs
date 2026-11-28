namespace NeNeCommander.Application.FileOperations;

/// <summary>Reports cleanup of the owned artifact link without claiming deletion of every possible alias.</summary>
public abstract record ArtifactLinkObservation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets a link retained by this operation; it does not assert its current pathname or exclude later external changes.</summary>
    public static ArtifactLinkObservation Retained { get; } = new RetainedCase();

    /// <summary>Gets an owned link whose removal was confirmed through its owned handle.</summary>
    public static ArtifactLinkObservation Removed { get; } = new RemovedCase();

    /// <summary>Gets an attempted cleanup whose result could not be established.</summary>
    public static ArtifactLinkObservation Uncertain { get; } = new UncertainCase();

    private ArtifactLinkObservation()
    {
    }

    private sealed record RetainedCase : ArtifactLinkObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record RemovedCase : ArtifactLinkObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record UncertainCase : ArtifactLinkObservation
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
