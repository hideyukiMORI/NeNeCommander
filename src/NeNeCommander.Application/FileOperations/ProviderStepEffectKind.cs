namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Identifies a provider-side effect that completed before the provider step returned a failure.
/// </summary>
public abstract record ProviderStepEffectKind
{
    /// <summary>
    /// Gets the effect indicating that the copy target entry now exists but its contents may be
    /// incomplete because the copy step failed.
    /// </summary>
    public static ProviderStepEffectKind CopyTargetCreated { get; } = new CopyTargetCreatedEffect();

    private ProviderStepEffectKind()
    {
    }

    private sealed record CopyTargetCreatedEffect : ProviderStepEffectKind;
}
