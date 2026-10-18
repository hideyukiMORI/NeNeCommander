namespace NeNeCommander.Application.FileOperations;

/// <summary>Contains a normalized verification rejection and no deletion evidence.</summary>
public sealed record CopyVerificationFailed : CopyVerificationOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal CopyVerificationFailed(FileOperationFailureKind failure)
    {
        Failure = failure;
    }

    /// <inheritdoc />
    public override FileOperationFailureKind Failure { get; }
}
