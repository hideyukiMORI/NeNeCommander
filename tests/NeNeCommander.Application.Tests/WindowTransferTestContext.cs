using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

internal static class WindowTransferTestContext
{
    internal static WindowDisplay Source { get; } = Display(10, WindowBounds.Create(-1920, 0, 1920, 1080), 1.25d);

    internal static WindowDisplay Target { get; } = Display(20, WindowBounds.Create(0, 0, 2560, 1400), 1.75d);

    internal static WindowDisplaySnapshot Displays { get; } = WindowDisplaySnapshot.Create(Source, [Target, Source]);

    internal static WindowPlacement Original { get; } = Placement(
        WindowBounds.Create(-1800, 100, 800, 600), Displays, WindowSizeConstraint.Create(1.25d, 0, 0), WindowPresenterState.Restored);

    internal static WindowTransferPlan Plan { get; } = Transfer(Original, WindowAdjustmentAction.NextDisplay, Displays);

    internal static WindowDisplay Display(ulong identity, WindowBounds area, double scale)
    {
        return WindowDisplay.Create(WindowDisplayIdentity.Create(identity), area, scale);
    }

    internal static WindowPlacement Placement(
        WindowBounds bounds, WindowDisplaySnapshot displays, WindowSizeConstraint minimum, WindowPresenterState state)
    {
        return WindowPlacement.Create(bounds, state, minimum,
            WindowWorkAreas.Create(displays.Current.WorkArea, [.. displays.Select(display => display.WorkArea)]));
    }

    internal static WindowTransferPlan Transfer(
        WindowPlacement placement, WindowAdjustmentAction action, WindowDisplaySnapshot displays)
    {
        return Assert.IsInstanceOfType<WindowTransferPlan>(
            WindowAdjustmentPlanner.Plan(placement, action, WindowTransferInput.Create(displays)));
    }

    internal static WindowPlacement At(WindowDisplaySnapshot displays, WindowBounds bounds)
    {
        return Placement(bounds, displays, Original.SizeConstraint, WindowPresenterState.Restored);
    }
}
