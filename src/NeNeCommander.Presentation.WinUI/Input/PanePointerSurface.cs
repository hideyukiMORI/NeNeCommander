namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Identifies passive pane hit targets after interactive descendants have been excluded.</summary>
public abstract record PanePointerSurface
{
    /// <summary>Closes concrete surface cases outside this assembly.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the passive pane header.</summary>
    public static PanePointerSurface Header { get; } = new HeaderSurface();

    /// <summary>Gets empty file-list background.</summary>
    public static PanePointerSurface ListBackground { get; } = new ListBackgroundSurface();

    private PanePointerSurface()
    {
    }

    private sealed record HeaderSurface : PanePointerSurface
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record ListBackgroundSurface : PanePointerSurface
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
