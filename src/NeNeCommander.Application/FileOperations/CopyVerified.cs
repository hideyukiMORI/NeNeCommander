namespace NeNeCommander.Application.FileOperations;

/// <summary>Contains immutable evidence of one successfully compared copied tree.</summary>
public sealed record CopyVerified : CopyVerificationOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal CopyVerified(VerifiedCopyReceipt receipt)
    {
        Receipt = receipt;
    }

    /// <summary>Gets the successful evidence to revalidate for source deletion.</summary>
    public VerifiedCopyReceipt Receipt { get; }
    /// <inheritdoc />
    public override FileOperationFailureKind? Failure => null;
}
