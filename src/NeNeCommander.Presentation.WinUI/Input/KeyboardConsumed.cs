namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents an event consumed without emitting an Application intent.</summary>
public sealed record KeyboardConsumed : KeyboardMappingOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal KeyboardConsumed()
    {
    }
}
