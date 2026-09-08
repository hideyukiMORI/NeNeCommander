namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Names the closed Presentation-owned selection action of one palette key.</summary>
public abstract record CommandPaletteKeyAction
{
    /// <summary>Gets the action that selects the preceding filtered command.</summary>
    public static CommandPaletteKeyAction MovePrevious { get; } = new MovePreviousAction();

    /// <summary>Gets the action that selects the following filtered command.</summary>
    public static CommandPaletteKeyAction MoveNext { get; } = new MoveNextAction();

    /// <summary>Gets the action that submits the selected command when available.</summary>
    public static CommandPaletteKeyAction Execute { get; } = new ExecuteAction();

    /// <summary>Gets the action that cancels the current palette.</summary>
    public static CommandPaletteKeyAction Cancel { get; } = new CancelAction();

    private CommandPaletteKeyAction()
    {
    }

    private sealed record MovePreviousAction : CommandPaletteKeyAction;
    private sealed record MoveNextAction : CommandPaletteKeyAction;
    private sealed record ExecuteAction : CommandPaletteKeyAction;
    private sealed record CancelAction : CommandPaletteKeyAction;
}
