using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Windowing;
using NeNeCommander.Presentation.WinUI.Windowing;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves planned, not-applied, uncertain and immediately observed transfers use different complete labels.</summary>
[TestClass]
public sealed class WindowTransferPresenterTests
{
    /// <summary>Declares both directions and every externally meaningful apply classification.</summary>
    public static System.Collections.Generic.IEnumerable<object[]> EffectCases =>
    [
        [WindowAdjustmentAction.PreviousDisplay, WindowTransferEffect.Observed, "WindowAdjustmentMovedPreviousDisplay", "TextPrimaryBrush"],
        [WindowAdjustmentAction.NextDisplay, WindowTransferEffect.Observed, "WindowAdjustmentMovedNextDisplay", "TextPrimaryBrush"],
        [WindowAdjustmentAction.PreviousDisplay, WindowTransferEffect.NotApplied, "WindowAdjustmentTransferNotApplied", "StatusWarningBrush"],
        [WindowAdjustmentAction.NextDisplay, WindowTransferEffect.NotApplied, "WindowAdjustmentTransferNotApplied", "StatusWarningBrush"],
        [WindowAdjustmentAction.PreviousDisplay, WindowTransferEffect.Unconfirmed, "WindowAdjustmentTransferUnconfirmed", "StatusWarningBrush"],
        [WindowAdjustmentAction.NextDisplay, WindowTransferEffect.Unconfirmed, "WindowAdjustmentTransferUnconfirmed", "StatusWarningBrush"],
    ];

    /// <summary>Both transfer directions show planned text, then only observed results can say moved.</summary>
    [TestMethod]
    [DynamicData(nameof(EffectCases))]
    public void PresentWhenTransferEffectChangesUsesExactOutcomeAndTone(
        WindowAdjustmentAction action, WindowTransferEffect effect, string label, string tone)
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentPlanned planned = Plan(session, action, WindowTransferInput.Create(Displays()));
        WindowAdjustmentPresentation before = WindowAdjustmentPresenter.Present(planned.Owner);
        Assert.AreEqual(action == WindowAdjustmentAction.PreviousDisplay
            ? "WindowAdjustmentPlannedPreviousDisplay" : "WindowAdjustmentPlannedNextDisplay", before.OutcomeResourceKey);
        Assert.AreEqual("TextPrimaryBrush", before.OutcomeBrushResourceKey);

        WindowAdjustmentOpen reported = Assert.IsInstanceOfType<WindowAdjustmentOpen>(session.ReportTransfer(planned.Owner, effect));
        WindowAdjustmentPresentation after = WindowAdjustmentPresenter.Present(reported);

        Assert.AreEqual(label, after.OutcomeResourceKey);
        Assert.AreEqual(tone, after.OutcomeBrushResourceKey);
    }
    /// <summary>Each transfer refusal has a separate full explanation and warning tone.</summary>
    [TestMethod]
    public void PresentWhenTransferIsRefusedShowsSpecificReason()
    {
        AssertRefusal(WindowTransferInput.Unavailable, "WindowAdjustmentRefusedDisplayInformation");
        AssertRefusal(WindowTransferInput.Hidden, "WindowAdjustmentRefusedHidden");
        WindowDisplaySnapshot displays = Displays();
        AssertRefusal(WindowTransferInput.Create(WindowDisplaySnapshot.Create(displays.Current, [displays.Current])), "WindowAdjustmentRefusedNoOtherDisplay");
        WindowDisplay tiny = WindowDisplay.Create(displays[1].Identity, WindowBounds.Create(2000, 0, 1, 1), 1d);
        AssertRefusal(WindowTransferInput.Create(WindowDisplaySnapshot.Create(displays.Current, [displays.Current, tiny])), "WindowAdjustmentRefusedTargetCannotFit");
    }

    private static void AssertRefusal(WindowTransferInput input, string label)
    {
        WindowAdjustmentSession session = new();
        WindowAdjustmentPlanned planned = Plan(session, WindowAdjustmentAction.NextDisplay, input);
        WindowAdjustmentPresentation presentation = WindowAdjustmentPresenter.Present(planned.Owner);
        Assert.AreEqual(label, presentation.OutcomeResourceKey);
        Assert.AreEqual("StatusWarningBrush", presentation.OutcomeBrushResourceKey);
    }

    private static WindowAdjustmentPlanned Plan(WindowAdjustmentSession session, WindowAdjustmentAction action, WindowTransferInput input)
    {
        WindowDisplaySnapshot displays = input is WindowTransferAvailable available ? available.Displays : Displays();
        WindowBounds[] areas = new WindowBounds[displays.Count];
        for (int index = 0; index < displays.Count; index++)
        {
            areas[index] = displays[index].WorkArea;
        }
        WindowPlacement placement = WindowPlacement.Create(WindowBounds.Create(0, 0, 800, 600), WindowPresenterState.Restored,
            WindowSizeConstraint.Create(1d, 0, 0), WindowWorkAreas.Create(displays.Current.WorkArea, areas));
        WindowAdjustmentOpen open = Assert.IsInstanceOfType<WindowAdjustmentOpen>(session.Open(PaneSide.Left, InteractionOwnership.ScopeOwnsInput));
        return Assert.IsInstanceOfType<WindowAdjustmentPlanned>(session.Adjust(WindowAdjustmentRequest.Create(open, action, placement, input)));
    }

    private static WindowDisplaySnapshot Displays()
    {
        WindowDisplay source = WindowDisplay.Create(WindowDisplayIdentity.Create(1), WindowBounds.Create(0, 0, 1920, 1080), 1.25d);
        WindowDisplay target = WindowDisplay.Create(WindowDisplayIdentity.Create(2), WindowBounds.Create(1920, 0, 2560, 1440), 1.75d);
        return WindowDisplaySnapshot.Create(source, [source, target]);
    }
}
