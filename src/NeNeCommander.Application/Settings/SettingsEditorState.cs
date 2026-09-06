namespace NeNeCommander.Application.Settings;

/// <summary>Identifies whether the session-owned settings editor is closed or open.</summary>
public abstract record SettingsEditorState
{
    /// <summary>Gets the closed editor state.</summary>
    public static SettingsEditorState Closed { get; } = new ClosedState();

    /// <summary>Gets the open modal editor state.</summary>
    public static SettingsEditorState Open { get; } = new OpenState();

    private SettingsEditorState()
    {
    }

    private sealed record ClosedState : SettingsEditorState;
    private sealed record OpenState : SettingsEditorState;
}
