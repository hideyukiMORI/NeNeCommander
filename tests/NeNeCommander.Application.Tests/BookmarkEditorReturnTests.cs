using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves keyboard continuation after saving and cancelling bookmark drafts.</summary>
[TestClass]
public sealed class BookmarkEditorReturnTests
{
    /// <summary>Proves a new entry is visible and selected even when the old search excluded it.</summary>
    [TestMethod]
    public void SaveNewBookmarkSelectsTheSavedEntryAndClearsTheOldSearch()
    {
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        BookmarkRegistrationDefaults defaults = new("New folder", "C:\\new");
        _ = editor.Apply(BookmarkEditorAction.Search("old"), BookmarkCatalog.Empty, defaults);
        _ = editor.Apply(BookmarkEditorAction.BeginAddBookmark, BookmarkCatalog.Empty, defaults);

        BookmarkEditorTransition.CatalogChanged saved = Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
            editor.Apply(BookmarkEditorAction.Save, BookmarkCatalog.Empty, defaults));

        BookmarksBrowsing browsing = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.AreEqual(string.Empty, browsing.Context.SearchText);
        Assert.AreEqual(new BookmarkSelection(saved.Catalog.Bookmarks[0]), browsing.Context.Selection);
    }

    /// <summary>Proves editing the name and category returns to the replacement rather than an old key.</summary>
    [TestMethod]
    public void SaveEditedBookmarkSelectsTheReplacementInItsNewCategory()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkEntry entry = BookmarkEditorTestValues.Entry("Before", "C:\\before", work);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([work], [entry]);
        BookmarkRegistrationDefaults defaults = BookmarkEditorTestValues.EmptyDefaults();
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = editor.Apply(BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(work)), catalog, defaults);
        _ = editor.Apply(BookmarkEditorAction.BeginEditBookmark(new BookmarkSelection(entry)), catalog, defaults);
        _ = editor.Apply(BookmarkEditorAction.UpdateBookmark(
            new BookmarkDraft("After", "C:\\after", BookmarkCategoryFilter.Uncategorized, null)), catalog, defaults);

        BookmarkEditorTransition.CatalogChanged saved = Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
            editor.Apply(BookmarkEditorAction.Save, catalog, defaults));

        BookmarksBrowsing browsing = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.AreSame(BookmarkCategoryFilter.Uncategorized, browsing.Context.Filter);
        Assert.AreEqual(new BookmarkSelection(saved.Catalog.Bookmarks[0]), browsing.Context.Selection);
    }

    /// <summary>Proves cancelling preserves the original query, filter and selected entry.</summary>
    [TestMethod]
    public void CancelDraftKeepsTheBrowseContext()
    {
        BookmarkEntry entry = BookmarkEditorTestValues.Entry("Original", "C:\\original", null);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([], [entry]);
        BookmarkRegistrationDefaults defaults = BookmarkEditorTestValues.EmptyDefaults();
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = editor.Apply(BookmarkEditorAction.Search("Original"), catalog, defaults);
        _ = editor.Apply(BookmarkEditorAction.Filter(BookmarkCategoryFilter.Uncategorized), catalog, defaults);
        _ = editor.Apply(BookmarkEditorAction.Select(new BookmarkSelection(entry)), catalog, defaults);
        BookmarksBrowsing before = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        _ = editor.Apply(BookmarkEditorAction.BeginEditBookmark(new BookmarkSelection(entry)), catalog, defaults);
        _ = editor.Apply(BookmarkEditorAction.UpdateBookmark(
            new BookmarkDraft("Lost", "C:\\lost", BookmarkCategoryFilter.Uncategorized, null)), catalog, defaults);

        _ = editor.Apply(BookmarkEditorAction.Cancel, catalog, defaults);

        Assert.AreEqual(before, editor.Current);
    }
}
