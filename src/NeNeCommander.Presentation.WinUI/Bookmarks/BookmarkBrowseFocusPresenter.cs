using System;
using System.Linq;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>Distinguishes the focused bookmark region from the other region's retained selection.</summary>
internal static class BookmarkBrowseFocusPresenter
{
    internal static BookmarkBrowsePresentation Present(BookmarkBrowsePresentation browse, BookmarkManagerRegion? focused)
    {
        ArgumentNullException.ThrowIfNull(browse);
        return new BookmarkBrowsePresentation(
            browse.SearchText,
            browse.Categories.Select(category => category with
            {
                SelectionMark = Mark(BookmarkManagerRegion.Categories, focused),
            }).ToList(),
            browse.Rows.Select(row => row with
            {
                SelectionMark = Mark(BookmarkManagerRegion.Bookmarks, focused),
            }).ToList(),
            browse.EmptyContent);
    }

    private static PaneRowMark Mark(BookmarkManagerRegion region, BookmarkManagerRegion? focused)
    {
        return region == focused ? PaneRowMark.FocusInActivePane : PaneRowMark.FocusInPassivePane;
    }
}
