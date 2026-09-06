using System;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Input;

/// <summary>Requests direct navigation through one of the nine fixed bookmark slots.</summary>
public sealed record BookmarkShortcutSelection : UserIntent
{
    internal BookmarkShortcutSelection(BookmarkShortcutSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        Slot = slot;
    }

    /// <summary>Gets the fixed slot to resolve from the current catalog.</summary>
    public BookmarkShortcutSlot Slot { get; }
}
