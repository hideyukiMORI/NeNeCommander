namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Identifies a completed provider-side effect independently of the step completion.
/// </summary>
public abstract record ProviderStepEffectKind
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>
    /// Gets the effect indicating that the copy target entry now exists but its contents may be
    /// incomplete because the copy step failed.
    /// </summary>
    public static ProviderStepEffectKind CopyTargetCreated { get; } = new CopyTargetCreatedEffect();

    /// <summary>Gets a confirmed recycle recorded by the provider.</summary>
    public static ProviderStepEffectKind Recycled { get; } = new RecycledEffect();

    /// <summary>Gets confirmed descendant removal before verified source deletion stopped.</summary>
    public static ProviderStepEffectKind SourcePartiallyDeleted { get; } = new SourcePartiallyDeletedEffect();

    private ProviderStepEffectKind()
    {
    }

    private sealed record CopyTargetCreatedEffect : ProviderStepEffectKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record RecycledEffect : ProviderStepEffectKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record SourcePartiallyDeletedEffect : ProviderStepEffectKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
