using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents the session-owned command palette interaction state.</summary>
public abstract record CommandPaletteState
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the closed state with no pending focus effect.</summary>
    public static CommandPaletteState Closed { get; } = new CommandPaletteClosed(null);

    private protected CommandPaletteState()
    {
    }

    internal static CommandPaletteState CloseFocusing(PaneSide side)
    {
        return new CommandPaletteClosed(side);
    }
}
