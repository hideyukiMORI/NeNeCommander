namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Names focus-only transitions owned by the canonical keyboard map.</summary>
public abstract record KeyboardFocusAction
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets entry into the existing application command buttons.</summary>
    public static KeyboardFocusAction CommandEntry { get; } = new CommandEntryAction();

    /// <summary>Gets return to the currently active pane without changing its selection.</summary>
    public static KeyboardFocusAction ActivePane { get; } = new ActivePaneAction();

    private KeyboardFocusAction()
    {
    }

    private sealed record CommandEntryAction : KeyboardFocusAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record ActivePaneAction : KeyboardFocusAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
