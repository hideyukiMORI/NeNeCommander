using System;

namespace NeNeCommander.Presentation.WinUI.Windowing;

/// <summary>
/// Represents what the caption bar renders for one presenter state and window activity: the title
/// brush, which glyph the middle button shows and the localized name that button carries.
/// </summary>
public sealed record CaptionBarPresentation
{
    internal CaptionBarPresentation(
        string titleBrushResourceKey,
        CaptionMiddleButton middleButton)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titleBrushResourceKey);
        ArgumentNullException.ThrowIfNull(middleButton);
        TitleBrushResourceKey = titleBrushResourceKey;
        MiddleButton = middleButton;
    }

    /// <summary>Gets the semantic brush resource of the application name.</summary>
    public string TitleBrushResourceKey { get; }

    /// <summary>Gets the maximize or restore form of the middle caption button.</summary>
    public CaptionMiddleButton MiddleButton { get; }
}
