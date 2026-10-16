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

    /// <summary>Gets a confirmed recycle completed before a later provider failure.</summary>
    public static ProviderStepEffectKind Recycled { get; } = new RecycledEffect();

    /// <summary>Gets confirmed descendant removal before verified source deletion stopped.</summary>
    public static ProviderStepEffectKind SourcePartiallyDeleted { get; } = new SourcePartiallyDeletedEffect();

    private ProviderStepEffectKind()
    {
    }

    private sealed record CopyTargetCreatedEffect : ProviderStepEffectKind;
    private sealed record RecycledEffect : ProviderStepEffectKind;
    private sealed record SourcePartiallyDeletedEffect : ProviderStepEffectKind;
}
