namespace NeNeCommander.Application.Settings;

/// <summary>Owns one theme picker and the settings surface to which it returns.</summary>
public sealed record ColorSchemePickerOpen : SettingsEditorState
{
    internal ColorSchemePickerOpen(SettingsEditorState returnTo)
    {
        ReturnTo = returnTo;
    }

    /// <summary>Gets the existing editor state restored after accepting or cancelling the picker.</summary>
    public SettingsEditorState ReturnTo { get; }

    private protected override void RestrictDerivation()
    {
    }
}
