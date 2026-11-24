namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents a canonical focus transition that emits no Application command.</summary>
public sealed record MappedKeyboardFocusAction : KeyboardMappingOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal MappedKeyboardFocusAction(KeyboardFocusAction action)
    {
        Action = action;
    }

    /// <summary>Gets the focus transition the host applies once.</summary>
    public KeyboardFocusAction Action { get; }
}
