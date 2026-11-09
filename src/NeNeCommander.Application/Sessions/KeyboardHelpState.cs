using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents the session-owned keyboard help surface state (ADR-0076).</summary>
public abstract record KeyboardHelpState
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the closed state with no pending focus effect.</summary>
    public static KeyboardHelpState Closed { get; } = new KeyboardHelpClosed(null);

    private protected KeyboardHelpState()
    {
    }

    internal static KeyboardHelpState CloseFocusing(PaneSide side)
    {
        return new KeyboardHelpClosed(side);
    }
}
