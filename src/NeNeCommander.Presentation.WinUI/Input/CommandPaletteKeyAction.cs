namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Names the closed Presentation-owned selection action of one palette key.</summary>
public abstract record CommandPaletteKeyAction
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the action that selects the preceding filtered command.</summary>
    public static CommandPaletteKeyAction MovePrevious { get; } = new MovePreviousAction();

    /// <summary>Gets the action that selects the following filtered command.</summary>
    public static CommandPaletteKeyAction MoveNext { get; } = new MoveNextAction();

    /// <summary>Gets the action that submits the selected command when available.</summary>
    public static CommandPaletteKeyAction Execute { get; } = new ExecuteAction();

    /// <summary>Gets the action that cancels the current palette.</summary>
    public static CommandPaletteKeyAction Cancel { get; } = new CancelAction();

    /// <summary>Gets the action that moves native focus to the other palette tab stop.</summary>
    public static CommandPaletteKeyAction MoveFocus { get; } = new MoveFocusAction();

    private CommandPaletteKeyAction()
    {
    }

    private sealed record MovePreviousAction : CommandPaletteKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record MoveNextAction : CommandPaletteKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record ExecuteAction : CommandPaletteKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record CancelAction : CommandPaletteKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record MoveFocusAction : CommandPaletteKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
