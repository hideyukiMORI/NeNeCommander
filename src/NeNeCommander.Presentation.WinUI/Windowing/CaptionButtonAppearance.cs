using System;

namespace NeNeCommander.Presentation.WinUI.Windowing;

/// <summary>Represents the semantic brushes of one caption button's surface and glyph.</summary>
public sealed record CaptionButtonAppearance
{
    internal CaptionButtonAppearance(string surfaceBrushResourceKey, string glyphBrushResourceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surfaceBrushResourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(glyphBrushResourceKey);
        SurfaceBrushResourceKey = surfaceBrushResourceKey;
        GlyphBrushResourceKey = glyphBrushResourceKey;
    }

    /// <summary>Gets the semantic brush resource of the button surface.</summary>
    public string SurfaceBrushResourceKey { get; }

    /// <summary>Gets the semantic brush resource of the button glyph.</summary>
    public string GlyphBrushResourceKey { get; }
}
