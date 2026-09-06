namespace NeNeCommander.Application.Settings;

/// <summary>Identifies the observed settings-directory effect of one rejected write attempt.</summary>
public abstract record SettingsDirectoryEffect
{
    /// <summary>Gets the state for an attempt that did not start directory creation.</summary>
    public static SettingsDirectoryEffect NotAttempted { get; } = new NotAttemptedEffect();

    /// <summary>Gets the state for a creation call followed by a verified safe parent chain.</summary>
    public static SettingsDirectoryEffect CreationObserved { get; } = new CreationObservedEffect();

    /// <summary>Gets the state for a creation call whose resulting parent chain is not confirmed.</summary>
    public static SettingsDirectoryEffect CreationUnconfirmed { get; } = new CreationUnconfirmedEffect();

    private SettingsDirectoryEffect()
    {
    }

    private sealed record NotAttemptedEffect : SettingsDirectoryEffect;
    private sealed record CreationObservedEffect : SettingsDirectoryEffect;
    private sealed record CreationUnconfirmedEffect : SettingsDirectoryEffect;
}
