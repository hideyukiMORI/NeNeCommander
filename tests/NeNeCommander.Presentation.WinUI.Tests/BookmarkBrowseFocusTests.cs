using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves focus emphasis moves without losing the two independent selections.</summary>
[TestClass]
public sealed class BookmarkBrowseFocusTests
{
    /// <summary>Proves only the focused region has the active mark and search leaves both passive.</summary>
    [TestMethod]
    public void RegionFocusChangesMarksWithoutChangingSelections()
    {
        BookmarkEntry entry = BookmarkPresenterTestContext.Entry("Repo", "C:\\repo", null, null);
        BookmarkRow row = new(new BookmarkSelection(entry), "Uncategorized", "KeyLabelUnmapped", BookmarkOptionSelection.Selected);
        BookmarkCategoryOption category = new(BookmarkCategoryFilter.All, "All", null, BookmarkOptionSelection.Selected);
        BookmarkBrowsePresentation browse = new("", [category], [row], BookmarkEmptyContent.Hidden);

        foreach (BookmarkManagerRegion? region in new[] { BookmarkManagerRegion.Bookmarks, BookmarkManagerRegion.Categories, null })
        {
            BookmarkBrowsePresentation focused = BookmarkBrowseFocusPresenter.Present(browse, region);

            Assert.AreEqual(row.Selection, focused.SelectedRow?.Selection);
            Assert.AreEqual(category.Filter, focused.Categories[0].Filter);
            Assert.IsTrue(focused.Categories[0].IsSelected);
            Assert.AreSame(region == BookmarkManagerRegion.Bookmarks ? PaneRowMark.FocusInActivePane : PaneRowMark.FocusInPassivePane,
                focused.Rows[0].Mark);
            Assert.AreSame(region == BookmarkManagerRegion.Categories ? PaneRowMark.FocusInActivePane : PaneRowMark.FocusInPassivePane,
                focused.Categories[0].Mark);
        }
    }
}
