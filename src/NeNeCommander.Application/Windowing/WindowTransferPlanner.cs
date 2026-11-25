using System.Collections.Generic;
using System.Linq;

namespace NeNeCommander.Application.Windowing;

/// <summary>Chooses one geometric destination for the canonical window planner without reading platform state.</summary>
internal static class WindowTransferPlanner
{
    internal static WindowAdjustmentPlan Plan(
        WindowPlacement placement,
        WindowAdjustmentAction action,
        WindowTransferInput input)
    {
        if (input == WindowTransferInput.Hidden)
        {
            return new WindowRefusedPlan(WindowAdjustmentRefusal.WindowIsHidden);
        }
        if (input is not WindowTransferAvailable available || !available.Displays.MatchesPlacement(placement))
        {
            return new WindowRefusedPlan(WindowAdjustmentRefusal.DisplayInformationUnavailable);
        }
        List<WindowDisplay> destinations = [.. available.Displays
            .GroupBy(display => display.WorkArea)
            .Select(group => group.First())
            .OrderBy(display => display.WorkArea.Left)
            .ThenBy(display => display.WorkArea.Top)
            .ThenBy(display => display.WorkArea.Width)
            .ThenBy(display => display.WorkArea.Height)];
        if (destinations.Count == 1)
        {
            return new WindowRefusedPlan(WindowAdjustmentRefusal.NoOtherDisplay);
        }
        int current = destinations.FindIndex(display => display.WorkArea == available.Displays.Current.WorkArea);
        int target = action == WindowAdjustmentAction.PreviousDisplay
            ? Previous(current, destinations.Count)
            : Next(current, destinations.Count);
        return WindowTransferSizing.Plan(placement, available.Displays, destinations[target]);
    }

    private static int Previous(int current, int count)
    {
        return current == 0 ? count - 1 : current - 1;
    }

    private static int Next(int current, int count)
    {
        return current == count - 1 ? 0 : current + 1;
    }
}
