using System;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>
/// Decides the settings editor's next focused item for one movement intent (ADR-0077). Items are
/// ordered as the editor shows them; movement stops at both ends and never wraps.
/// </summary>
public static class SettingsFocusOrder
{
    /// <summary>Returns the index to focus after one movement intent.</summary>
    /// <param name="current">Index of the focused item, or a negative value when no item has focus.</param>
    /// <param name="count">Number of focusable items in display order.</param>
    /// <param name="intent">Movement intent the settings context mapped.</param>
    /// <returns>The index to focus, or a negative value when there is no item.</returns>
    public static int Move(int current, int count, UserIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0)
        {
            return -1;
        }
        if (current < 0 || current >= count)
        {
            return 0;
        }
        int step = intent == UserIntent.MoveNext ? 1 : intent == UserIntent.MovePrevious ? -1 : 0;
        return Math.Clamp(current + step, 0, count - 1);
    }
}
