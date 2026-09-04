namespace NeNeCommander.Application.Settings;

/// <summary>
/// Identifies the closed light or dark appearance of one color scheme so the host can align
/// framework-provided control styling with the scheme's own surfaces.
/// </summary>
public abstract record ColorSchemeAppearance
{
    /// <summary>Gets the appearance of a scheme whose surfaces are darker than its text.</summary>
    public static ColorSchemeAppearance Dark { get; } = new DarkAppearance();

    /// <summary>Gets the appearance of a scheme whose surfaces are lighter than its text.</summary>
    public static ColorSchemeAppearance Light { get; } = new LightAppearance();

    private ColorSchemeAppearance()
    {
    }

    private sealed record DarkAppearance : ColorSchemeAppearance;
    private sealed record LightAppearance : ColorSchemeAppearance;
}
