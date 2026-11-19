using System;
using System.Collections.Generic;
using System.Linq;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Holds the bookmark manager's part of the canonical key map (ADR-0080). It is consulted only by
/// <see cref="KeyboardIntentMapper.Map"/> and published only through
/// <see cref="KeyboardIntentMapper.BookmarkManagerBindings"/>, so the declarations exist once and
/// every displayed bookmark hint reads them (KBD-005).
/// </summary>
internal static class BookmarkManagerKeyMap
{
    /// <summary>
    /// The browse context owns movement, region switching, activation, search entry, the row and
    /// category commands and closing; the search-field context owns only the three keys that return
    /// to the list. Drafts own Control+Enter and Escape. <c>Tab</c> also stands for <c>Shift+Tab</c>
    /// because the translator does not report Shift and the two regions make both directions the
    /// same move.
    /// </summary>
    internal static IReadOnlyList<BookmarkManagerKeyBinding> Bindings { get; } =
        Array.AsReadOnly<BookmarkManagerKeyBinding>(
    [
        new(KeyboardContext.Bookmarks, KeyboardKey.J, KeyboardModifier.None, BookmarkManagerKeyAction.MoveNext),
        new(KeyboardContext.Bookmarks, KeyboardKey.Down, KeyboardModifier.None, BookmarkManagerKeyAction.MoveNext),
        new(KeyboardContext.Bookmarks, KeyboardKey.K, KeyboardModifier.None, BookmarkManagerKeyAction.MovePrevious),
        new(KeyboardContext.Bookmarks, KeyboardKey.Up, KeyboardModifier.None, BookmarkManagerKeyAction.MovePrevious),
        new(KeyboardContext.Bookmarks, KeyboardKey.Enter, KeyboardModifier.None, BookmarkManagerKeyAction.Activate),
        new(KeyboardContext.Bookmarks, KeyboardKey.Slash, KeyboardModifier.None, BookmarkManagerKeyAction.FocusSearch),
        new(KeyboardContext.Bookmarks, KeyboardKey.Tab, KeyboardModifier.None, BookmarkManagerKeyAction.SwitchRegion),
        new(KeyboardContext.Bookmarks, KeyboardKey.A, KeyboardModifier.None, BookmarkManagerKeyAction.Add),
        new(KeyboardContext.Bookmarks, KeyboardKey.E, KeyboardModifier.None, BookmarkManagerKeyAction.Edit),
        new(KeyboardContext.Bookmarks, KeyboardKey.D, KeyboardModifier.None, BookmarkManagerKeyAction.Delete),
        new(KeyboardContext.Bookmarks, KeyboardKey.Escape, KeyboardModifier.None, BookmarkManagerKeyAction.Close),
        new(KeyboardContext.BookmarkSearch, KeyboardKey.Escape, KeyboardModifier.None, BookmarkManagerKeyAction.LeaveSearch),
        new(KeyboardContext.BookmarkSearch, KeyboardKey.Down, KeyboardModifier.None, BookmarkManagerKeyAction.LeaveSearch),
        new(KeyboardContext.BookmarkSearch, KeyboardKey.Enter, KeyboardModifier.None, BookmarkManagerKeyAction.LeaveSearch),
        new(KeyboardContext.BookmarkDraft, KeyboardKey.Enter, KeyboardModifier.Control, BookmarkManagerKeyAction.Save),
        new(KeyboardContext.BookmarkDraft, KeyboardKey.Escape, KeyboardModifier.None, BookmarkManagerKeyAction.Close),
    ]);

    /// <summary>Returns whether one context is a bookmark-manager context.</summary>
    internal static bool Owns(KeyboardContext context)
    {
        return context == KeyboardContext.Bookmarks || context == KeyboardContext.BookmarkSearch ||
            context == KeyboardContext.BookmarkDraft;
    }

    /// <summary>
    /// Maps one key while the bookmark manager owns input. A declared key/modifier pair yields its
    /// action; a held key repeats only movement, so a held <c>Enter</c>, <c>a</c>, <c>e</c>,
    /// <c>d</c>, <c>Tab</c>, <c>/</c> or <c>Escape</c> acts once and its repeats are consumed. An
    /// undeclared key/modifier pair passes through as in the settings context,
    /// so the search field receives its text and the lists keep their own keys.
    /// </summary>
    internal static KeyboardMappingOutcome Map(KeyboardInput input)
    {
        if (input.Context == KeyboardContext.BookmarkDraft && input.Key == KeyboardKey.Enter &&
            input.RepeatState == KeyRepeatState.Repeated)
        {
            return new KeyboardConsumed();
        }
        BookmarkManagerKeyBinding? binding = Bindings.FirstOrDefault(binding =>
            binding.Context == input.Context && binding.Key == input.Key && binding.Modifier == input.Modifier);
        return binding is null
            ? new KeyboardPassThrough()
            : input.RepeatState == KeyRepeatState.Repeated && !binding.Action.Repeats
                ? new KeyboardConsumed()
                : new MappedBookmarkManagerAction(binding.Action);
    }
}
