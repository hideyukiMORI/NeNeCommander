namespace NeNeCommander.Presentation.WinUI.Windowing;

/// <summary>
/// Represents the closed activation of the application window as the host observes it. The caption
/// bar draws its title and glyphs differently while the window is inactive (ADR-0078).
/// </summary>
public abstract record WindowActivity
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the activity of the foreground window.</summary>
    public static WindowActivity Active { get; } = new ActiveWindow();

    /// <summary>Gets the activity of a window that is not the foreground window.</summary>
    public static WindowActivity Inactive { get; } = new InactiveWindow();

    private WindowActivity()
    {
    }

    private sealed record ActiveWindow : WindowActivity
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record InactiveWindow : WindowActivity
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
