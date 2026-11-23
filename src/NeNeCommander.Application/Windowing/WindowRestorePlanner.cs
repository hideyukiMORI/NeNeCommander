using System;
using System.Linq;

namespace NeNeCommander.Application.Windowing;

/// <summary>Fits a startup normal rectangle into one currently attached physical-pixel work area.</summary>
public static class WindowRestorePlanner
{
    /// <summary>Centers a first launch or corrects saved geometry without reading or changing the desktop.</summary>
    public static SavedWindowPlacement Plan(
        SavedWindowPlacement? saved,
        WindowBounds initial,
        WindowWorkAreas workAreas)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(workAreas);
        WindowBounds requested = saved?.Bounds ?? initial;
        WindowBounds area = saved is null ? workAreas.Current : SelectWorkArea(requested, workAreas);
        int width = Math.Min(requested.Width, area.Width);
        int height = Math.Min(requested.Height, area.Height);
        long left = saved is null ? (long)area.Left + ((area.Width - width) / 2) : requested.Left;
        long top = saved is null ? (long)area.Top + ((area.Height - height) / 2) : requested.Top;
        WindowBounds corrected = WindowBounds.Create(
            checked((int)Math.Clamp(left, area.Left, (long)area.Left + area.Width - width)),
            checked((int)Math.Clamp(top, area.Top, (long)area.Top + area.Height - height)),
            width,
            height);
        return SavedWindowPlacement.Create(corrected, saved?.State ?? WindowPresenterState.Restored);
    }

    /// <summary>Projects a decided normal placement into the existing one-shot window operation model.</summary>
    public static WindowAdjustmentPlan PlanNormalBounds(SavedWindowPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        return new WindowResizePlan(placement.Bounds);
    }

    private static WindowBounds SelectWorkArea(WindowBounds requested, WindowWorkAreas workAreas)
    {
        return workAreas.Attached
            .OrderByDescending(area => Overlap(requested, area))
            .ThenBy(area => DistanceSquared(requested, area))
            .ThenBy(area => area.Left)
            .ThenBy(area => area.Top)
            .ThenBy(area => area.Width)
            .ThenBy(area => area.Height)
            .First();
    }

    private static long Overlap(WindowBounds bounds, WindowBounds area)
    {
        long width = Math.Max(0L, Math.Min((long)bounds.Left + bounds.Width, (long)area.Left + area.Width) - Math.Max(bounds.Left, area.Left));
        long height = Math.Max(0L, Math.Min((long)bounds.Top + bounds.Height, (long)area.Top + area.Height) - Math.Max(bounds.Top, area.Top));
        return width * height;
    }

    private static double DistanceSquared(WindowBounds bounds, WindowBounds area)
    {
        double horizontal = Math.Max(0L, Math.Max((long)area.Left - bounds.Left - bounds.Width, (long)bounds.Left - area.Left - area.Width));
        double vertical = Math.Max(0L, Math.Max((long)area.Top - bounds.Top - bounds.Height, (long)bounds.Top - area.Top - area.Height));
        return (horizontal * horizontal) + (vertical * vertical);
    }
}
