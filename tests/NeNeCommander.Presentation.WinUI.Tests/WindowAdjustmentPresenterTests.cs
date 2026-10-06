using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Windowing;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Windowing;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the helper's projection: its title, outcome label and tone, and hints generated from the key table.</summary>
[TestClass]
public sealed class WindowAdjustmentPresenterTests
{
    private static readonly WindowBounds Desktop = WindowBounds.Create(0, 0, 1920, 1040);

    /// <summary>
    /// Proves hints are one per group in declaration order, with the group's displayed caps in
    /// declaration order: move shows four caps and no alias shows a cap.
    /// </summary>
    [TestMethod]
    public void PresentWhenHintsAreGeneratedGroupsTheKeyTableCaps()
    {
        IReadOnlyList<WindowAdjustmentKeyHint> hints = WindowAdjustmentKeyHintPresenter.Present();

        Assert.HasCount(6, hints);
        AssertHint(hints[0], "WindowAdjustmentHintMove", "KeyLabelH", "KeyLabelJ", "KeyLabelK", "KeyLabelL");
        AssertHint(hints[1], "WindowAdjustmentHintEnlarge", "KeyLabelPlus");
        AssertHint(hints[2], "WindowAdjustmentHintShrink", "KeyLabelMinus");
        AssertHint(hints[3], "WindowAdjustmentHintMaximize", "KeyLabelM");
        AssertHint(hints[4], "WindowAdjustmentHintRestore", "KeyLabelR");
        AssertHint(hints[5], "WindowAdjustmentHintLeave", "KeyLabelEscape");
        Assert.IsFalse(hints.SelectMany(hint => hint.KeyLabelResourceKeys).Any(label =>
            label is "KeyLabelLeft" or "KeyLabelDown" or "KeyLabelUp" or "KeyLabelRight" or "KeyLabelCtrlW"));
    }

    /// <summary>Proves every displayed cap comes from a table entry that declares its hint group.</summary>
    [TestMethod]
    public void PresentWhenHintsAreGeneratedShowEveryGroupedEntryExactlyOnce()
    {
        IReadOnlyList<WindowAdjustmentKeyHint> hints = WindowAdjustmentKeyHintPresenter.Present();
        WindowAdjustmentKeyBinding[] grouped =
        [
            .. KeyboardIntentMapper.WindowAdjustmentBindings.Where(binding => binding.HintGroup is not null),
        ];

        Assert.HasCount(grouped.Length, hints.SelectMany(hint => hint.KeyLabelResourceKeys));
        Assert.HasCount(6, grouped.Select(binding => binding.HintGroup).Distinct());
    }

    /// <summary>Proves each hint group names its own label resource.</summary>
    [TestMethod]
    public void LabelResourceKeyWhenHintGroupIsReadNamesItsResource()
    {
        Assert.AreEqual("WindowAdjustmentHintMove", WindowAdjustmentHintGroup.Move.LabelResourceKey);
        Assert.AreEqual("WindowAdjustmentHintEnlarge", WindowAdjustmentHintGroup.Enlarge.LabelResourceKey);
        Assert.AreEqual("WindowAdjustmentHintShrink", WindowAdjustmentHintGroup.Shrink.LabelResourceKey);
        Assert.AreEqual("WindowAdjustmentHintMaximize", WindowAdjustmentHintGroup.Maximize.LabelResourceKey);
        Assert.AreEqual("WindowAdjustmentHintRestore", WindowAdjustmentHintGroup.Restore.LabelResourceKey);
        Assert.AreEqual("WindowAdjustmentHintLeave", WindowAdjustmentHintGroup.Leave.LabelResourceKey);
    }

    /// <summary>Proves a freshly opened mode shows its title, the idle label in the secondary tone, and the hints.</summary>
    [TestMethod]
    public void PresentWhenNoActionHappenedShowsTheIdleOutcome()
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen open = Opened(session);

        WindowAdjustmentPresentation presentation = WindowAdjustmentPresenter.Present(open);

        Assert.AreEqual("WindowAdjustmentTitle", presentation.TitleResourceKey);
        Assert.AreEqual("WindowAdjustmentOutcomeIdle", presentation.OutcomeResourceKey);
        Assert.AreEqual("TextSecondaryBrush", presentation.OutcomeBrushResourceKey);
        AssertSameHints(WindowAdjustmentKeyHintPresenter.Present(), presentation.KeyHints);
    }

    /// <summary>Proves each planned action is labelled by the action's own name in the primary tone.</summary>
    [TestMethod]
    [DataRow("MoveLeft", "WindowAdjustmentPlannedMoveLeft")]
    [DataRow("MoveDown", "WindowAdjustmentPlannedMoveDown")]
    [DataRow("MoveUp", "WindowAdjustmentPlannedMoveUp")]
    [DataRow("MoveRight", "WindowAdjustmentPlannedMoveRight")]
    [DataRow("Enlarge", "WindowAdjustmentPlannedEnlarge")]
    [DataRow("Shrink", "WindowAdjustmentPlannedShrink")]
    [DataRow("Maximize", "WindowAdjustmentPlannedMaximize")]
    [DataRow("Restore", "WindowAdjustmentPlannedRestore")]
    public void PresentWhenActionWasPlannedShowsItsLabelInThePrimaryTone(string actionName, string label)
    {
        WindowAdjustmentAction action = ActionNamed(actionName);
        WindowPresenterState state = action == WindowAdjustmentAction.Restore
            ? WindowPresenterState.Maximized
            : WindowPresenterState.Restored;

        WindowAdjustmentPresentation presentation = WindowAdjustmentPresenter.Present(
            Adjusted(action, Placement(WindowBounds.Create(100, 100, 800, 600), state)));

        Assert.AreEqual(label, presentation.OutcomeResourceKey);
        Assert.AreEqual("TextPrimaryBrush", presentation.OutcomeBrushResourceKey);
    }

    /// <summary>Proves each refusal reason has its own label in the warning tone.</summary>
    [TestMethod]
    public void PresentWhenActionWasRefusedShowsTheReasonInTheWarningTone()
    {
        (WindowAdjustmentAction Action, WindowPlacement Placement, string Label)[] cases =
        [
            (WindowAdjustmentAction.MoveUp,
                Placement(WindowBounds.Create(100, 0, 800, 600), WindowPresenterState.Restored),
                "WindowAdjustmentRefusedCaption"),
            (WindowAdjustmentAction.Enlarge,
                Placement(WindowBounds.Create(0, 0, 1920, 1040), WindowPresenterState.Restored),
                "WindowAdjustmentRefusedMaximumSize"),
            (WindowAdjustmentAction.Shrink,
                Placement(WindowBounds.Create(0, 0, 40, 40), WindowPresenterState.Restored),
                "WindowAdjustmentRefusedMinimumSize"),
            (WindowAdjustmentAction.Maximize,
                Placement(WindowBounds.Create(0, 0, 800, 600), WindowPresenterState.Maximized),
                "WindowAdjustmentRefusedMaximized"),
            (WindowAdjustmentAction.MoveLeft,
                Placement(WindowBounds.Create(0, 0, 800, 600), WindowPresenterState.Minimized),
                "WindowAdjustmentRefusedMinimized"),
            (WindowAdjustmentAction.Restore,
                Placement(WindowBounds.Create(0, 0, 800, 600), WindowPresenterState.Restored),
                "WindowAdjustmentRefusedRestored"),
            (WindowAdjustmentAction.MoveRight,
                Placement(WindowBounds.Create(0, 0, 800, 600), WindowPresenterState.NotOverlapped),
                "WindowAdjustmentRefusedNotOverlapped"),
            (WindowAdjustmentAction.MoveDown, WindowPlacement.Unavailable, "WindowAdjustmentRefusedUnavailable"),
        ];
        HashSet<string> labels = [];

        foreach ((WindowAdjustmentAction action, WindowPlacement placement, string label) in cases)
        {
            WindowAdjustmentOpen open = Adjusted(action, placement);
            _ = Assert.IsInstanceOfType<WindowActionRefused>(open.Outcome);

            WindowAdjustmentPresentation presentation = WindowAdjustmentPresenter.Present(open);

            Assert.AreEqual(label, presentation.OutcomeResourceKey);
            Assert.AreEqual("StatusWarningBrush", presentation.OutcomeBrushResourceKey);
            Assert.IsTrue(labels.Add(presentation.OutcomeResourceKey));
        }
        Assert.HasCount(8, labels);
    }

    /// <summary>Proves the presenter and its values reject every absent or blank part.</summary>
    [TestMethod]
    public void ConstructWindowAdjustmentPresentationWhenAPartIsMissingThrows()
    {
        IReadOnlyList<WindowAdjustmentKeyHint> hints = [];

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowAdjustmentPresenter.Present(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowAdjustmentKeyHint(null!, "Label"));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new WindowAdjustmentKeyHint([], " "));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowAdjustmentKeyHint([], null!));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new WindowAdjustmentPresentation(" ", "O", "B", hints));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new WindowAdjustmentPresentation("T", " ", "B", hints));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new WindowAdjustmentPresentation("T", "O", " ", hints));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowAdjustmentPresentation("T", "O", "B", null!));
    }

    private static WindowAdjustmentOpen Opened(WindowAdjustmentSession session)
    {
        return Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            session.Open(PaneSide.Left, InteractionOwnership.ScopeOwnsInput));
    }

    private static WindowAdjustmentOpen Adjusted(WindowAdjustmentAction action, WindowPlacement placement)
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentOpen open = Opened(session);
        _ = Assert.IsInstanceOfType<WindowAdjustmentPlanned>(
            session.Adjust(WindowAdjustmentRequest.Create(open, action, placement)));
        return Assert.IsInstanceOfType<WindowAdjustmentOpen>(session.Current);
    }

    private static WindowPlacement Placement(WindowBounds bounds, WindowPresenterState state)
    {
        return WindowPlacement.Create(
            bounds,
            state,
            WindowSizeConstraint.Create(1d, 0, 0),
            WindowWorkAreas.Create(Desktop, [Desktop]));
    }

    private static WindowAdjustmentAction ActionNamed(string name)
    {
        return (WindowAdjustmentAction)typeof(WindowAdjustmentAction).GetProperty(name)!.GetValue(null)!;
    }

    private static void AssertHint(WindowAdjustmentKeyHint hint, string label, params string[] caps)
    {
        Assert.AreEqual(label, hint.IntentLabelResourceKey);
        CollectionAssert.AreEqual(caps, hint.KeyLabelResourceKeys.ToArray());
    }

    private static void AssertSameHints(
        IReadOnlyList<WindowAdjustmentKeyHint> expected,
        IReadOnlyList<WindowAdjustmentKeyHint> actual)
    {
        Assert.HasCount(expected.Count, actual);
        for (int index = 0; index < expected.Count; index++)
        {
            Assert.AreEqual(expected[index].IntentLabelResourceKey, actual[index].IntentLabelResourceKey);
            CollectionAssert.AreEqual(
                expected[index].KeyLabelResourceKeys.ToArray(),
                actual[index].KeyLabelResourceKeys.ToArray());
        }
    }
}
