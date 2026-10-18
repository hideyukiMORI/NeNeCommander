namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents a key consumed by the Presentation-owned palette selection state.</summary>
public sealed record MappedCommandPaletteAction : KeyboardMappingOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal MappedCommandPaletteAction(CommandPaletteKeyAction action)
    {
        Action = action;
    }

    /// <summary>Gets the palette selection action.</summary>
    public CommandPaletteKeyAction Action { get; }
}
