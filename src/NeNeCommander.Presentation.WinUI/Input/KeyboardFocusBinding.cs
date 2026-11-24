namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Declares one focus transition and its localized hint within the canonical key map.</summary>
public sealed record KeyboardFocusBinding
{
    internal KeyboardFocusBinding(KeyboardContext context, KeyboardKey key, KeyboardFocusAction action)
    {
        Context = context;
        Key = key;
        Action = action;
    }

    /// <summary>Gets the context that owns this unmodified key.</summary>
    public KeyboardContext Context { get; }

    /// <summary>Gets the key that requests focus.</summary>
    public KeyboardKey Key { get; }

    /// <summary>Gets the focus transition.</summary>
    public KeyboardFocusAction Action { get; }

    /// <summary>Gets the localized description of the declared transition.</summary>
    public string LabelResourceKey => Action == KeyboardFocusAction.CommandEntry ? "CommandEntryFocus" : "CommandEntryReturn";
}
