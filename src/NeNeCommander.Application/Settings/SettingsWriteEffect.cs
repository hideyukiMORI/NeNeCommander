namespace NeNeCommander.Application.Settings;

/// <summary>Identifies the temporary-artifact residue of one rejected write attempt.</summary>
public abstract record SettingsWriteEffect
{
    /// <summary>Gets the state for an attempt that left no owned temporary artifact.</summary>
    public static SettingsWriteEffect None { get; } = new NoEffect();

    /// <summary>Gets the state for an attempt whose owned temporary artifact could not be removed.</summary>
    public static SettingsWriteEffect TemporaryArtifactLeft { get; } = new TemporaryArtifactLeftEffect();

    private SettingsWriteEffect()
    {
    }

    private sealed record NoEffect : SettingsWriteEffect;
    private sealed record TemporaryArtifactLeftEffect : SettingsWriteEffect;
}
