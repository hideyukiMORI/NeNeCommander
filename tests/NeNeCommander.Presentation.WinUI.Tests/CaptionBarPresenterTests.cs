using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;
using NeNeCommander.Presentation.WinUI.Windowing;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the caption bar's appearance table of ADR-0078 and the design handoff of 2026-11-15.</summary>
[TestClass]
public sealed class CaptionBarPresenterTests
{
    /// <summary>Proves the middle button restores only while the presenter reports maximized.</summary>
    [TestMethod]
    public void PresentWhenMaximizedShowsRestoreAndOtherwiseMaximize()
    {
        Assert.AreSame(CaptionMiddleButton.Restore, CaptionBarPresenter.Present(WindowPresenterState.Maximized, WindowActivity.Active).MiddleButton);
        Assert.AreSame(CaptionMiddleButton.Maximize, CaptionBarPresenter.Present(WindowPresenterState.Restored, WindowActivity.Active).MiddleButton);
        Assert.AreSame(CaptionMiddleButton.Maximize, CaptionBarPresenter.Present(WindowPresenterState.Minimized, WindowActivity.Active).MiddleButton);
        Assert.AreSame(CaptionMiddleButton.Maximize, CaptionBarPresenter.Present(WindowPresenterState.NotOverlapped, WindowActivity.Active).MiddleButton);
        Assert.AreSame(CaptionMiddleButton.Maximize, CaptionBarPresenter.Present(WindowPresenterState.Unavailable, WindowActivity.Active).MiddleButton);
    }

    /// <summary>Proves the middle button's accessible name follows its form through localized resources.</summary>
    [TestMethod]
    public void MiddleButtonWhenFormedNamesItsLocalizedAction()
    {
        Assert.AreEqual("CaptionMaximizeName", CaptionMiddleButton.Maximize.NameResourceKey);
        Assert.AreEqual("CaptionRestoreName", CaptionMiddleButton.Restore.NameResourceKey);
    }

    /// <summary>Proves the application name uses the secondary text while active and the hidden text while inactive.</summary>
    [TestMethod]
    public void PresentWhenActivityChangesSelectsTheTitleBrush()
    {
        Assert.AreEqual("TextSecondaryBrush", CaptionBarPresenter.Present(WindowPresenterState.Restored, WindowActivity.Active).TitleBrushResourceKey);
        Assert.AreEqual("TextHiddenBrush", CaptionBarPresenter.Present(WindowPresenterState.Restored, WindowActivity.Inactive).TitleBrushResourceKey);
    }

    /// <summary>
    /// Proves the button table: at rest the window surface with the key-hint glyph, or the hidden
    /// glyph while inactive; pointer over the pane surface and pressed the border surface, both with
    /// the primary glyph whatever the activity. No state names a danger brush, so close never turns red.
    /// </summary>
    [TestMethod]
    [DataRow("Rest", "Active", "SurfaceWindowBrush", "TextKeyHintBrush")]
    [DataRow("Rest", "Inactive", "SurfaceWindowBrush", "TextHiddenBrush")]
    [DataRow("Hovered", "Active", "SurfacePaneBrush", "TextPrimaryBrush")]
    [DataRow("Hovered", "Inactive", "SurfacePaneBrush", "TextPrimaryBrush")]
    [DataRow("Pressed", "Active", "BorderSubtleBrush", "TextPrimaryBrush")]
    [DataRow("Pressed", "Inactive", "BorderSubtleBrush", "TextPrimaryBrush")]
    public void PresentButtonWhenStateIsGivenSelectsTheDesignedBrushes(
        string buttonState,
        string activity,
        string surface,
        string glyph)
    {
        CaptionButtonAppearance appearance = CaptionBarPresenter.PresentButton(ButtonState(buttonState), Activity(activity));

        Assert.AreEqual(surface, appearance.SurfaceBrushResourceKey);
        Assert.AreEqual(glyph, appearance.GlyphBrushResourceKey);
        Assert.DoesNotContain("Danger", appearance.SurfaceBrushResourceKey);
        Assert.DoesNotContain("Danger", appearance.GlyphBrushResourceKey);
    }

    /// <summary>Proves missing inputs are defects rather than a silent default appearance.</summary>
    [TestMethod]
    public void PresentWhenAnInputIsMissingThrows()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CaptionBarPresenter.Present(null!, WindowActivity.Active));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CaptionBarPresenter.Present(WindowPresenterState.Restored, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CaptionBarPresenter.PresentButton(null!, WindowActivity.Active));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CaptionBarPresenter.PresentButton(CaptionButtonPointer.Rest, null!));
    }

    private static CaptionButtonPointer ButtonState(string name)
    {
        return name switch
        {
            "Rest" => CaptionButtonPointer.Rest,
            "Hovered" => CaptionButtonPointer.Hovered,
            "Pressed" => CaptionButtonPointer.Pressed,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
    }

    private static WindowActivity Activity(string name)
    {
        return name switch
        {
            "Active" => WindowActivity.Active,
            "Inactive" => WindowActivity.Inactive,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
    }
}
