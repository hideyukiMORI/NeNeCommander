using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Pairs the explicit provider completion with its complete immutable receipt.</summary>
public sealed record ProviderStepOutcome
{
    private ProviderStepOutcome(ProviderStepCompletion completion, ProviderStepReceipt receipt)
    {
        Completion = completion;
        Receipt = receipt;
    }

    /// <summary>Gets the explicit success, failure or cancellation of this provider step.</summary>
    public ProviderStepCompletion Completion { get; }

    /// <summary>Gets the failure, or absence for success and cancellation.</summary>
    public FileOperationFailureKind? Failure => Completion.Failure;

    /// <summary>Gets the facts retained on success, failure and cancellation.</summary>
    public ProviderStepReceipt Receipt { get; }

    /// <summary>Creates the sole outcome shape with a mandatory receipt, including when it is empty.</summary>
    public static ProviderStepOutcome Create(ProviderStepCompletion completion, ProviderStepReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(receipt);
        return new ProviderStepOutcome(completion, receipt);
    }
}
