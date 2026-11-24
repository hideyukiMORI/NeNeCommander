namespace NeNeCommander.Application.Launching;

/// <summary>Distinguishes activation from reveal within the existing external-handoff lifecycle.</summary>
public abstract record FileLaunchPurpose
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets normal activation through the target's associated application.</summary>
    public static FileLaunchPurpose Activation { get; } = new ActivationPurpose();

    /// <summary>Gets selection of the exact target in its Windows Shell parent.</summary>
    public static FileLaunchPurpose Reveal { get; } = new RevealPurpose();

    private FileLaunchPurpose()
    {
    }

    private sealed record ActivationPurpose : FileLaunchPurpose
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record RevealPurpose : FileLaunchPurpose
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
