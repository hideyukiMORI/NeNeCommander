using System.Collections.Generic;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Retains the mapper's accepted single-shot presses across input-owner changes. Each identified
/// key is released by its next initial event; other keys and raw events do not release it.
/// Palette execution and non-repeating bookmark actions share this one mechanism.
/// </summary>
internal sealed class KeyboardRepeatGuard
{
    private readonly HashSet<KeyboardKey> _singleShotKeys = [];

    internal bool Consumes(KeyboardInput input)
    {
        if (input.RepeatState == KeyRepeatState.Initial)
        {
            _ = _singleShotKeys.Remove(input.Key);
            return false;
        }
        return _singleShotKeys.Contains(input.Key);
    }

    internal void Observe(KeyboardInput input, KeyboardMappingOutcome outcome)
    {
        bool singleShot = outcome switch
        {
            MappedCommandPaletteAction palette => palette.Action == CommandPaletteKeyAction.Execute,
            MappedKeyboardIntent mapped => mapped.Intent == UserIntent.OpenCurrentLocationInOtherPane,
            MappedBookmarkManagerAction bookmark => !bookmark.Action.Repeats,
            KeyboardPassThrough => input.Context == KeyboardContext.BookmarkDraft && input.Key == KeyboardKey.Enter,
            _ => false
        };
        if (singleShot)
        {
            _ = _singleShotKeys.Add(input.Key);
        }
    }

    internal void ObserveNative(KeyboardInput input)
    {
        if (input.Key == KeyboardKey.Enter || input.Key == KeyboardKey.Escape)
        {
            _ = _singleShotKeys.Add(input.Key);
        }
    }
}
