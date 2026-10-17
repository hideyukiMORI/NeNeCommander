using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Reports normalized verification failure or the immutable evidence needed for source deletion.</summary>
public abstract record CopyVerificationOutcome
{
    private protected CopyVerificationOutcome()
    {
    }

    /// <summary>Gets a normalized failure, or absence after successful verification.</summary>
    public abstract FileOperationFailureKind? Failure { get; }

    /// <summary>Creates a successful verification with complete evidence.</summary>
    public static CopyVerificationOutcome Succeeded(VerifiedCopyReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return new CopyVerified(receipt);
    }

    /// <summary>Creates a failed verification that cannot authorize deletion.</summary>
    public static CopyVerificationOutcome Failed(FileOperationFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new CopyVerificationFailed(failure);
    }
}
