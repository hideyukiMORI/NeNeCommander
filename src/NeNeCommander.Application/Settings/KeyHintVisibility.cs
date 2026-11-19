namespace NeNeCommander.Application.Settings;

/// <summary>Identifies whether the normal footer shortcut list is shown.</summary>
public abstract record KeyHintVisibility
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the choice showing every normal footer hint.</summary>
    public static KeyHintVisibility Shown { get; } = new ShownVisibility();

    /// <summary>Gets the choice retaining only help and required modal guidance.</summary>
    public static KeyHintVisibility Hidden { get; } = new HiddenVisibility();

    private KeyHintVisibility()
    {
    }

    private sealed record ShownVisibility : KeyHintVisibility
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record HiddenVisibility : KeyHintVisibility
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
