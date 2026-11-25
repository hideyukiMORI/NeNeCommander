using System;
using System.Linq;

namespace NeNeCommander.Application.Windowing;

/// <summary>Qualifies fresh adapter facts for each bounded transfer effect without reading platform state.</summary>
public static class WindowTransferQualification
{
    /// <summary>Checks the complete source observation immediately before any changing call.</summary>
    /// <param name="plan">Exact plan returned for the current owner.</param>
    /// <param name="placement">Fresh physical placement and declared minima.</param>
    /// <param name="input">Fresh visibility and complete display facts.</param>
    /// <returns>Whether the immutable facts still authorize the first planned effect.</returns>
    public static bool CanStart(WindowTransferPlan plan, WindowPlacement placement, WindowTransferInput input)
    {
        Validate(plan, placement, input);
        return MatchesWindow(plan, placement) && placement.Bounds == plan.Source.Bounds &&
            input is WindowTransferAvailable available && available.Displays.MatchesPlacement(placement) &&
            available.Displays.Current == plan.Displays.Current && available.Displays.SequenceEqual(plan.Displays);
    }

    /// <summary>Checks destination facts before the final call, independently of stale XAML scale.</summary>
    /// <param name="plan">Exact plan whose first call has begun.</param>
    /// <param name="placement">Fresh placement and unchanged declared minima.</param>
    /// <param name="input">Fresh visible display facts, with the intended destination current.</param>
    /// <returns>Whether these facts still authorize the one final planned effect.</returns>
    public static bool CanFinish(WindowTransferPlan plan, WindowPlacement placement, WindowTransferInput input)
    {
        Validate(plan, placement, input);
        return MatchesWindow(plan, placement) && input is WindowTransferAvailable available &&
            available.Displays.MatchesPlacement(placement) && available.Displays.Current == plan.Target;
    }

    /// <summary>Checks the exact final bounds together with every destination qualification.</summary>
    /// <param name="plan">Exact applied plan.</param>
    /// <param name="placement">Immediately observed physical placement.</param>
    /// <param name="input">Immediately observed visible display facts.</param>
    /// <returns>Whether the planned final state was observed; later OS stability is not promised.</returns>
    public static bool HasObservedResult(WindowTransferPlan plan, WindowPlacement placement, WindowTransferInput input)
    {
        Validate(plan, placement, input);
        return CanFinish(plan, placement, input) && placement.Bounds == plan.Geometry.Final;
    }

    private static bool MatchesWindow(WindowTransferPlan plan, WindowPlacement placement)
    {
        return placement.PresenterState == WindowPresenterState.Restored &&
            placement.SizeConstraint.PreferredMinimumWidth == plan.Source.SizeConstraint.PreferredMinimumWidth &&
            placement.SizeConstraint.PreferredMinimumHeight == plan.Source.SizeConstraint.PreferredMinimumHeight;
    }

    private static void Validate(WindowTransferPlan plan, WindowPlacement placement, WindowTransferInput input)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(input);
    }
}
