using System;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Presentation.WinUI.Windowing;

/// <summary>
/// Projects the observed presenter state, window activity and button pointer states into the
/// caption bar's semantic brushes and middle-button form (ADR-0078). It decides only appearance:
/// the presenter state is read from the window each time and never stored here.
/// </summary>
public static class CaptionBarPresenter
{
    private const string RestSurface = "SurfaceWindowBrush";
    private const string HoveredSurface = "SurfacePaneBrush";
    private const string PressedSurface = "BorderSubtleBrush";
    private const string HighlightedGlyph = "TextPrimaryBrush";
    private const string ActiveGlyph = "TextKeyHintBrush";
    private const string ActiveTitle = "TextSecondaryBrush";
    private const string InactiveText = "TextHiddenBrush";

    /// <summary>Creates the caption bar presentation of one presenter state and window activity.</summary>
    /// <param name="state">Presenter state the window reports now.</param>
    /// <param name="activity">Activation the window reported last.</param>
    /// <returns>The title brush and the middle button form.</returns>
    public static CaptionBarPresentation Present(WindowPresenterState state, WindowActivity activity)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(activity);
        return new CaptionBarPresentation(
            activity == WindowActivity.Active ? ActiveTitle : InactiveText,
            state == WindowPresenterState.Maximized ? CaptionMiddleButton.Restore : CaptionMiddleButton.Maximize);
    }

    /// <summary>
    /// Creates one caption button's brushes. Pointer over and pressed use the pane and border
    /// surfaces with the primary text glyph; at rest the glyph uses the key-hint color, or the hidden
    /// color while the window is inactive. The close button follows the same rule as the others.
    /// </summary>
    /// <param name="buttonState">Pointer state of the button.</param>
    /// <param name="activity">Activation the window reported last.</param>
    /// <returns>The button's surface and glyph brushes.</returns>
    public static CaptionButtonAppearance PresentButton(CaptionButtonPointer buttonState, WindowActivity activity)
    {
        ArgumentNullException.ThrowIfNull(buttonState);
        ArgumentNullException.ThrowIfNull(activity);
        return buttonState == CaptionButtonPointer.Pressed
            ? new CaptionButtonAppearance(PressedSurface, HighlightedGlyph)
            : buttonState == CaptionButtonPointer.Hovered
                ? new CaptionButtonAppearance(HoveredSurface, HighlightedGlyph)
                : new CaptionButtonAppearance(
                    RestSurface,
                    activity == WindowActivity.Active ? ActiveGlyph : InactiveText);
    }
}
