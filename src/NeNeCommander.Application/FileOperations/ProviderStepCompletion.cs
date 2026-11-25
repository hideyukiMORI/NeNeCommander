using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Distinguishes an incomplete cancelled provider step from success and expected failure.</summary>
public abstract record ProviderStepCompletion
{
    private ProviderStepCompletion()
    {
    }

    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the completion for a fully completed provider step.</summary>
    public static ProviderStepCompletion Succeeded { get; } = new SucceededStep();

    /// <summary>Gets the completion for an interrupted provider step whose receipt retains every observed effect.</summary>
    public static ProviderStepCompletion Cancelled { get; } = new CancelledStep();

    /// <summary>Gets the expected failure, or absence for success and cancellation.</summary>
    public abstract FileOperationFailureKind? Failure { get; }

    /// <summary>Creates the completion for one expected provider failure.</summary>
    public static ProviderStepCompletion Failed(FileOperationFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new FailedStep(failure);
    }

    private sealed record SucceededStep : ProviderStepCompletion
    {
        public override FileOperationFailureKind? Failure => null;
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record CancelledStep : ProviderStepCompletion
    {
        public override FileOperationFailureKind? Failure => null;
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record FailedStep : ProviderStepCompletion
    {
        internal FailedStep(FileOperationFailureKind failure)
        {
            Failure = failure;
        }

        public override FileOperationFailureKind Failure { get; }
        private protected override void RestrictDerivation()
        {
        }
    }
}
