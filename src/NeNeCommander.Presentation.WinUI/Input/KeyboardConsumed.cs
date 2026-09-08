namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents an event consumed without emitting an Application intent.</summary>
public sealed record KeyboardConsumed : KeyboardMappingOutcome
{
    internal KeyboardConsumed()
    {
    }
}
