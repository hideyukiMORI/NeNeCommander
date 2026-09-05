using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a closed provider-step success or expected failure.
/// </summary>
public sealed record ProviderStepOutcome
{
    private ProviderStepOutcome(FileOperationFailureKind? failure, ProviderStepEffectKind? effect)
    {
        Failure = failure;
        Effect = effect;
    }

    /// <summary>Gets the failure, or absence when the step succeeded.</summary>
    public FileOperationFailureKind? Failure { get; }

    /// <summary>Gets the exact provider-side effect completed before failure, or absence.</summary>
    public ProviderStepEffectKind? Effect { get; }

    /// <summary>Creates a successful provider-step outcome.</summary>
    /// <returns>The canonical success outcome.</returns>
    public static ProviderStepOutcome Succeeded()
    {
        return new ProviderStepOutcome(null, null);
    }

    /// <summary>Creates a failed provider-step outcome.</summary>
    /// <param name="failure">Normalized expected failure.</param>
    /// <returns>A failed outcome.</returns>
    public static ProviderStepOutcome Failed(FileOperationFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new ProviderStepOutcome(failure, null);
    }

    /// <summary>Creates a failed outcome that reports one exact provider-side effect.</summary>
    /// <param name="failure">Normalized expected failure.</param>
    /// <param name="effect">Effect completed before the failure.</param>
    /// <returns>A failed outcome retaining the completed effect.</returns>
    public static ProviderStepOutcome FailedAfterEffect(
        FileOperationFailureKind failure,
        ProviderStepEffectKind effect)
    {
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentNullException.ThrowIfNull(effect);
        return new ProviderStepOutcome(failure, effect);
    }
}
