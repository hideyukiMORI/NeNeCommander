namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the closed result of parsing a bookmark shortcut slot number.</summary>
public abstract record BookmarkShortcutSlotParseOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected BookmarkShortcutSlotParseOutcome()
    {
    }
}
