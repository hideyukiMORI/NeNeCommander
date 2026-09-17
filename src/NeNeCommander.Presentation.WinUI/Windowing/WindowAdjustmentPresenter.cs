using System;
using System.Collections.Generic;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Presentation.WinUI.Windowing;

/// <summary>
/// Projects the Application-owned open window-adjustment mode into the helper the host renders. It
/// owns no resources, no geometry, and no decision: it names the outcome the mode already decided.
/// </summary>
public static class WindowAdjustmentPresenter
{
    private static readonly Dictionary<WindowAdjustmentAction, string> PlannedLabels = new()
    {
        [WindowAdjustmentAction.MoveLeft] = "WindowAdjustmentPlannedMoveLeft",
        [WindowAdjustmentAction.MoveDown] = "WindowAdjustmentPlannedMoveDown",
        [WindowAdjustmentAction.MoveUp] = "WindowAdjustmentPlannedMoveUp",
        [WindowAdjustmentAction.MoveRight] = "WindowAdjustmentPlannedMoveRight",
        [WindowAdjustmentAction.Enlarge] = "WindowAdjustmentPlannedEnlarge",
        [WindowAdjustmentAction.Shrink] = "WindowAdjustmentPlannedShrink",
        [WindowAdjustmentAction.Maximize] = "WindowAdjustmentPlannedMaximize",
        [WindowAdjustmentAction.Restore] = "WindowAdjustmentPlannedRestore",
    };

    private static readonly Dictionary<WindowAdjustmentRefusal, string> RefusedLabels = new()
    {
        [WindowAdjustmentRefusal.CaptionWouldLeaveDesktop] = "WindowAdjustmentRefusedCaption",
        [WindowAdjustmentRefusal.AtMaximumSize] = "WindowAdjustmentRefusedMaximumSize",
        [WindowAdjustmentRefusal.AtMinimumSize] = "WindowAdjustmentRefusedMinimumSize",
        [WindowAdjustmentRefusal.WindowIsMaximized] = "WindowAdjustmentRefusedMaximized",
        [WindowAdjustmentRefusal.WindowIsMinimized] = "WindowAdjustmentRefusedMinimized",
        [WindowAdjustmentRefusal.WindowIsRestored] = "WindowAdjustmentRefusedRestored",
        [WindowAdjustmentRefusal.PresenterIsNotOverlapped] = "WindowAdjustmentRefusedNotOverlapped",
        [WindowAdjustmentRefusal.PlacementUnavailable] = "WindowAdjustmentRefusedUnavailable",
    };

    /// <summary>Creates the helper presentation of one Application-owned open mode.</summary>
    /// <param name="open">Exact open state the host is rendering.</param>
    /// <returns>The complete immutable helper presentation.</returns>
    public static WindowAdjustmentPresentation Present(WindowAdjustmentOpen open)
    {
        ArgumentNullException.ThrowIfNull(open);
        return new WindowAdjustmentPresentation(
            "WindowAdjustmentTitle",
            OutcomeResourceKey(open.Outcome),
            OutcomeBrushResourceKey(open.Outcome),
            WindowAdjustmentKeyHintPresenter.Present());
    }

    private static string OutcomeResourceKey(WindowAdjustmentOutcome outcome)
    {
        return outcome is WindowActionPlanned planned
            ? Declared(PlannedLabels, planned.Action)
            : outcome is WindowActionRefused refused
                ? Declared(RefusedLabels, refused.Reason)
                : "WindowAdjustmentOutcomeIdle";
    }

    private static string OutcomeBrushResourceKey(WindowAdjustmentOutcome outcome)
    {
        return outcome is WindowActionPlanned
            ? "TextPrimaryBrush"
            : outcome is WindowActionRefused ? "StatusWarningBrush" : "TextSecondaryBrush";
    }

    private static string Declared<TKey>(Dictionary<TKey, string> labels, TKey key)
        where TKey : notnull
    {
        return labels.TryGetValue(key, out string? label)
            ? label
            : throw new InvalidOperationException("The window adjustment outcome is not supported.");
    }
}
