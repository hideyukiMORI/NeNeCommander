namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents a consumed chord prefix awaiting its second key.</summary>
public sealed record KeyboardAwaitingChord : KeyboardMappingOutcome
{
    internal KeyboardAwaitingChord()
    {
    }
}
