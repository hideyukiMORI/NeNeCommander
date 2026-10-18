namespace NeNeCommander.Application.Settings;

/// <summary>Identifies whether the session-owned settings editor is closed or open.</summary>
public abstract record SettingsEditorState
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the closed editor state.</summary>
    public static SettingsEditorState Closed { get; } = new ClosedState();

    /// <summary>Gets the open modal editor state.</summary>
    public static SettingsEditorState Open { get; } = new OpenState();

    /// <summary>Gets the open bookmark-catalog editor state.</summary>
    public static SettingsEditorState Bookmarks { get; } = new BookmarksState();

    private SettingsEditorState()
    {
    }

    private sealed record ClosedState : SettingsEditorState
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record OpenState : SettingsEditorState
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record BookmarksState : SettingsEditorState
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
