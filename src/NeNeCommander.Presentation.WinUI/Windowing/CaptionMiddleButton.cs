namespace NeNeCommander.Presentation.WinUI.Windowing;

/// <summary>
/// Represents the closed form of the middle caption button: it maximizes a restored window and
/// restores a maximized one. Each form names its glyph and its localized accessible name.
/// </summary>
public abstract record CaptionMiddleButton
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the form shown while the window is not maximized.</summary>
    public static CaptionMiddleButton Maximize { get; } = new MaximizeForm();

    /// <summary>Gets the form shown while the window is maximized.</summary>
    public static CaptionMiddleButton Restore { get; } = new RestoreForm();

    private CaptionMiddleButton()
    {
    }

    /// <summary>Gets the localization resource of the button's accessible name.</summary>
    public abstract string NameResourceKey { get; }

    private sealed record MaximizeForm : CaptionMiddleButton
    {
        public override string NameResourceKey => "CaptionMaximizeName";

        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record RestoreForm : CaptionMiddleButton
    {
        public override string NameResourceKey => "CaptionRestoreName";

        private protected override void RestrictDerivation()
        {
        }
    }
}
