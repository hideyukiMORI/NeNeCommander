namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents an integer outside the nine fixed shortcut slots.</summary>
public sealed record BookmarkShortcutSlotRejected : BookmarkShortcutSlotParseOutcome
{
    private protected override void RestrictDerivation()
    {
    }
}
