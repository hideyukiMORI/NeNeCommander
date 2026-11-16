using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Derives the defaults of a new bookmark draft from the active pane: its listed location as the
/// path and the location's last segment as the name when that segment is a valid display name.
/// </summary>
internal static class ActivePaneBookmarkDefaults
{
    /// <summary>Derives the defaults from one dual-pane snapshot.</summary>
    internal static BookmarkRegistrationDefaults From(DualPaneSnapshot panes)
    {
        PaneSnapshot pane = panes.Of(panes.ActiveSide);
        if (pane.Content is not PaneContentListed listed)
        {
            return new BookmarkRegistrationDefaults(string.Empty, string.Empty);
        }
        string path = listed.Listing.Location.CanonicalText;
        string candidate = LeafName(path);
        string name = BookmarkDisplayName.Parse(candidate) is BookmarkDisplayNameAccepted accepted
            ? accepted.Name.Value
            : string.Empty;
        return new BookmarkRegistrationDefaults(name, path);
    }

    private static string LeafName(string path)
    {
        string withoutTrailingSeparator = path.TrimEnd('\\');
        int separator = withoutTrailingSeparator.LastIndexOf('\\');
        return separator < 0 ? string.Empty : withoutTrailingSeparator[(separator + 1)..];
    }
}
