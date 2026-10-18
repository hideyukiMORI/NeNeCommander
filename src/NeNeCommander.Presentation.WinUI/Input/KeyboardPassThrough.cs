namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents an event left to the focused control or framework.</summary>
public sealed record KeyboardPassThrough : KeyboardMappingOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal KeyboardPassThrough()
    {
    }
}
