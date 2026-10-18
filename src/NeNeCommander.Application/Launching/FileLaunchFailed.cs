namespace NeNeCommander.Application.Launching;

/// <summary>Represents one expected file handoff failure normalized by its provider adapter.</summary>
public sealed record FileLaunchFailed : FileLaunchOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal FileLaunchFailed(FileLaunchFailureKind failure)
    {
        Failure = failure;
    }

    /// <summary>Gets the normalized reason the provider did not accept the handoff.</summary>
    public FileLaunchFailureKind Failure { get; }
}
