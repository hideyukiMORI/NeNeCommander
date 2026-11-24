using System;
using System.Collections.Generic;
using System.Linq;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Holds the window-adjustment part of the canonical key map. Only KeyboardIntentMapper calls
/// this map and publishes its declaration list, which also supplies every window shortcut hint.
/// </summary>
internal static class WindowAdjustmentKeyMap
{
    /// <summary>
    /// The dedicated table of the window-adjustment mode. Geometry, scope leave and help paging share one declaration:
    /// each arrow is an alias of the Vim direction beside it and <c>Ctrl+W</c> is an alias of
    /// <c>Escape</c>, so an alias declares no hint group and shows no cap (KBD-005).
    /// </summary>
    internal static IReadOnlyList<WindowAdjustmentKeyBinding> Bindings { get; } =
        Array.AsReadOnly<WindowAdjustmentKeyBinding>(
    [
        new(KeyboardKey.H, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveLeft, WindowAdjustmentHintGroup.Move),
        new(KeyboardKey.Left, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveLeft, null),
        new(KeyboardKey.J, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveDown, WindowAdjustmentHintGroup.Move),
        new(KeyboardKey.Down, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveDown, null),
        new(KeyboardKey.K, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveUp, WindowAdjustmentHintGroup.Move),
        new(KeyboardKey.Up, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveUp, null),
        new(KeyboardKey.L, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveRight, WindowAdjustmentHintGroup.Move),
        new(KeyboardKey.Right, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveRight, null),
        new(KeyboardKey.P, KeyboardModifier.None, WindowAdjustmentKeyAction.PreviousDisplay, WindowAdjustmentHintGroup.PreviousDisplay),
        new(KeyboardKey.Left, KeyboardModifier.Control, WindowAdjustmentKeyAction.PreviousDisplay, WindowAdjustmentHintGroup.PreviousDisplay),
        new(KeyboardKey.N, KeyboardModifier.None, WindowAdjustmentKeyAction.NextDisplay, WindowAdjustmentHintGroup.NextDisplay),
        new(KeyboardKey.Right, KeyboardModifier.Control, WindowAdjustmentKeyAction.NextDisplay, WindowAdjustmentHintGroup.NextDisplay),
        new(KeyboardKey.Plus, KeyboardModifier.None, WindowAdjustmentKeyAction.Enlarge, WindowAdjustmentHintGroup.Enlarge),
        new(KeyboardKey.Minus, KeyboardModifier.None, WindowAdjustmentKeyAction.Shrink, WindowAdjustmentHintGroup.Shrink),
        new(KeyboardKey.M, KeyboardModifier.None, WindowAdjustmentKeyAction.Maximize, WindowAdjustmentHintGroup.Maximize),
        new(KeyboardKey.R, KeyboardModifier.None, WindowAdjustmentKeyAction.Restore, WindowAdjustmentHintGroup.Restore),
        new(KeyboardKey.Escape, KeyboardModifier.None, WindowAdjustmentKeyAction.Leave, WindowAdjustmentHintGroup.Leave),
        new(KeyboardKey.W, KeyboardModifier.Control, WindowAdjustmentKeyAction.Leave, null),
        new(KeyboardKey.PageUp, KeyboardModifier.None, WindowAdjustmentKeyAction.PreviousHelpPage, WindowAdjustmentHintGroup.ScrollHelp),
        new(KeyboardKey.PageDown, KeyboardModifier.None, WindowAdjustmentKeyAction.NextHelpPage, WindowAdjustmentHintGroup.ScrollHelp),
    ]);

    /// <summary>
    /// Maps one key while the window-adjustment mode owns input. A declared key yields its action;
    /// every other identified key is consumed, so no pane, editor, or native control sees it, and
    /// auto-repeat is accepted only for the actions a user holds down. The raw virtual-key event
    /// that precedes a produced character is passed through untouched, exactly as everywhere else
    /// in <see cref="KeyboardIntentMapper.Map"/>, because the mode's letters and symbols are produced characters
    /// (KBD-003) and a handled virtual-key event would suppress them.
    /// </summary>
    internal static KeyboardMappingOutcome Map(KeyboardInput input)
    {
        if (input.Key == KeyboardKey.Other)
        {
            return new KeyboardPassThrough();
        }
        WindowAdjustmentKeyBinding? binding = Bindings.FirstOrDefault(binding =>
            binding.Key == input.Key && binding.Modifier == input.Modifier);
        return binding is null || IsIgnoredWindowRepeat(binding.Action, input.RepeatState)
            ? new KeyboardConsumed()
            : new MappedWindowAdjustmentAction(binding.Action);
    }

    private static bool IsIgnoredWindowRepeat(
        WindowAdjustmentKeyAction action,
        KeyRepeatState repeatState)
    {
        return repeatState == KeyRepeatState.Repeated &&
            (action == WindowAdjustmentKeyAction.Maximize ||
                action == WindowAdjustmentKeyAction.Restore ||
                action == WindowAdjustmentKeyAction.Leave ||
                action == WindowAdjustmentKeyAction.PreviousDisplay ||
                action == WindowAdjustmentKeyAction.NextDisplay);
    }
}
