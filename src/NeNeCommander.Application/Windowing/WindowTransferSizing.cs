using System;

namespace NeNeCommander.Application.Windowing;

/// <summary>Calculates both transfer rectangles from reported scales, with no OS reads or unchecked narrowing.</summary>
internal static class WindowTransferSizing
{
    internal static WindowAdjustmentPlan Plan(
        WindowPlacement source,
        WindowDisplaySnapshot displays,
        WindowDisplay target)
    {
        double sourceScale = displays.Current.Scale;
        double targetScale = target.Scale;
        double width = Math.Round(source.Bounds.Width / sourceScale * targetScale, MidpointRounding.AwayFromZero);
        double height = Math.Round(source.Bounds.Height / sourceScale * targetScale, MidpointRounding.AwayFromZero);
        if (!IsPositiveFinite(width) || !IsPositiveFinite(height))
        {
            return new WindowRefusedPlan(WindowAdjustmentRefusal.TargetCannotFit);
        }
        width = Math.Min(width, target.WorkArea.Width);
        height = Math.Min(height, target.WorkArea.Height);
        double initialWidth = Math.Max(1d, Math.Round(width * sourceScale / targetScale, MidpointRounding.AwayFromZero));
        double initialHeight = Math.Max(1d, Math.Round(height * sourceScale / targetScale, MidpointRounding.AwayFromZero));
        long left = (long)target.WorkArea.Left + ((target.WorkArea.Width - (int)width) / 2);
        long top = (long)target.WorkArea.Top + ((target.WorkArea.Height - (int)height) / 2);
        if (!FitsCoordinates(left, top, width, height) || !FitsCoordinates(left, top, initialWidth, initialHeight) ||
            !RespectsMinimum(width, height, source.SizeConstraint, targetScale) ||
            !RespectsMinimum(initialWidth, initialHeight, source.SizeConstraint, sourceScale))
        {
            return new WindowRefusedPlan(WindowAdjustmentRefusal.TargetCannotFit);
        }
        WindowBounds initial = WindowBounds.Create((int)left, (int)top, (int)initialWidth, (int)initialHeight);
        WindowBounds final = WindowBounds.Create((int)left, (int)top, (int)width, (int)height);
        return new WindowTransferPlan(source, displays, target, WindowTransferGeometry.Create(initial, final));
    }

    private static bool IsPositiveFinite(double dimension)
    {
        return double.IsFinite(dimension) && dimension >= 1d;
    }

    private static bool FitsCoordinates(long left, long top, double width, double height)
    {
        // Origins are signed Int32 area origins plus nonnegative offsets; dimensions are positive.
        return width <= int.MaxValue && height <= int.MaxValue &&
            left + width <= int.MaxValue && top + height <= int.MaxValue;
    }

    private static bool RespectsMinimum(double width, double height, WindowSizeConstraint constraint, double scale)
    {
        return width >= Minimum(constraint.PreferredMinimumWidth, scale) &&
            height >= Minimum(constraint.PreferredMinimumHeight, scale);
    }

    private static double Minimum(int declared, double scale)
    {
        return declared > 0 ? declared : Math.Max(1d, Math.Round(32d * scale, MidpointRounding.AwayFromZero));
    }
}
