namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents an integer accepted as one of the nine fixed shortcut slots.</summary>
public sealed record BookmarkShortcutSlotAccepted : BookmarkShortcutSlotParseOutcome
{
    internal BookmarkShortcutSlotAccepted(BookmarkShortcutSlot slot)
    {
        Slot = slot;
    }

    /// <summary>Gets the accepted slot.</summary>
    public BookmarkShortcutSlot Slot { get; }
}
