using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a closed provider-step success or expected failure.
/// </summary>
public sealed record ProviderStepOutcome
{
    private ProviderStepOutcome(FileOperationFailureKind? failure)
    {
        Failure = failure;
    }

    /// <summary>Gets the failure, or absence when the step succeeded.</summary>
    public FileOperationFailureKind? Failure { get; }

    /// <summary>Creates a successful provider-step outcome.</summary>
    /// <returns>The canonical success outcome.</returns>
    public static ProviderStepOutcome Succeeded()
    {
        return new ProviderStepOutcome((FileOperationFailureKind?)null);
    }

    /// <summary>Creates a failed provider-step outcome.</summary>
    /// <param name="failure">Normalized expected failure.</param>
    /// <returns>A failed outcome.</returns>
    public static ProviderStepOutcome Failed(FileOperationFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new ProviderStepOutcome(failure);
    }
}
