using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves repeated native browse notifications cannot erase the saved-item notice.</summary>
[TestClass]
public sealed class BookmarkSavedNoticeTests
{
    /// <summary>Proves an unchanged effective browse input retains the new saved item and notice.</summary>
    [TestMethod]
    [DataRow("select")]
    [DataRow("search")]
    [DataRow("filter")]
    public void SameInputAfterAddingRetainsSelectionAndNotice(string input)
    {
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        BookmarkCatalog saved = SaveNew(editor);

        AssertRepeatedInputRetainsNotice(editor, saved, input);
    }

    /// <summary>Proves the replacement selected by an edit retains its notice across the same inputs.</summary>
    [TestMethod]
    [DataRow("select")]
    [DataRow("search")]
    [DataRow("filter")]
    public void SameInputAfterEditingRetainsSelectionAndNotice(string input)
    {
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        BookmarkCatalog catalog = SaveNew(editor);
        BookmarkSelection selected = RequireSaved(editor);
        BookmarkRegistrationDefaults defaults = BookmarkEditorTestValues.EmptyDefaults();
        _ = editor.Apply(BookmarkEditorAction.BeginEditBookmark(selected), catalog, defaults);
        _ = editor.Apply(BookmarkEditorAction.UpdateBookmark(new BookmarkDraft(
            "Renamed", "C:\\renamed", BookmarkCategoryFilter.Uncategorized, null)), catalog, defaults);
        BookmarkEditorTransition.CatalogChanged saved = Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
            editor.Apply(BookmarkEditorAction.Save, catalog, defaults));

        AssertRepeatedInputRetainsNotice(editor, saved.Catalog, input);
    }

    /// <summary>Proves actual browse changes end the saved-item notice.</summary>
    [TestMethod]
    [DataRow("selection")]
    [DataRow("clear")]
    [DataRow("search")]
    [DataRow("filter")]
    public void ChangedInputEndsNotice(string input)
    {
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        BookmarkCatalog catalog = SaveNew(editor);
        BookmarksBrowsing before = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        BookmarkEditorAction action = input switch
        {
            "selection" => BookmarkEditorAction.Select(new BookmarkSelection(catalog.Bookmarks[0])),
            "clear" => BookmarkEditorAction.Select(null),
            "search" => BookmarkEditorAction.Search("different"),
            "filter" => BookmarkEditorAction.Filter(BookmarkCategoryFilter.All),
            _ => throw new InvalidOperationException("Unknown browse change fixture."),
        };

        _ = editor.Apply(action, catalog, BookmarkEditorTestValues.EmptyDefaults());

        BookmarksBrowsing after = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.IsNull(after.SavedSelection);
        Assert.AreNotEqual(before.Context, after.Context);
    }

    /// <summary>Proves identical keys cannot bypass complete stale metadata validation.</summary>
    [TestMethod]
    public void RepeatedSelectionWithReplacedMetadataIsRejected()
    {
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = SaveNew(editor);
        BookmarkSelection stale = RequireSaved(editor);
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCatalog replacement = BookmarkEditorTestValues.Catalog(
            [work], [BookmarkEditorTestValues.Entry("Saved", "C:\\replacement", work)]);

        _ = editor.Apply(BookmarkEditorAction.Select(stale), replacement, BookmarkEditorTestValues.EmptyDefaults());

        BookmarksBrowsing after = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.AreSame(BookmarkEditorProblem.StaleSelection, after.Problem);
        Assert.IsNull(after.SavedSelection);
    }

    /// <summary>Proves repeating a deleted category still fails current-catalog validation.</summary>
    [TestMethod]
    public void RepeatedFilterWithRemovedCategoryIsRejected()
    {
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = SaveNew(editor);
        BookmarksBrowsing saved = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);

        _ = editor.Apply(BookmarkEditorAction.Filter(saved.Context.Filter), BookmarkCatalog.Empty,
            BookmarkEditorTestValues.EmptyDefaults());

        BookmarksBrowsing after = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.AreSame(BookmarkEditorProblem.InvalidCategory, after.Problem);
        Assert.IsNull(after.SavedSelection);
    }

    private static BookmarkCatalog SaveNew(BookmarkEditorSession editor)
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog(
            [work], [BookmarkEditorTestValues.Entry("Other", "C:\\other", work)]);
        BookmarkRegistrationDefaults defaults = new("Saved", "C:\\saved");
        _ = editor.Apply(BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(work)), catalog, defaults);
        _ = editor.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, defaults);
        return Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
            editor.Apply(BookmarkEditorAction.Save, catalog, defaults)).Catalog;
    }

    private static BookmarkSelection RequireSaved(BookmarkEditorSession editor)
    {
        BookmarksBrowsing saved = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        return saved.SavedSelection ?? throw new InvalidOperationException("The fixture must retain its saved item.");
    }

    private static void AssertRepeatedInputRetainsNotice(
        BookmarkEditorSession editor, BookmarkCatalog catalog, string input)
    {
        BookmarksBrowsing saved = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        BookmarkSelection selection = RequireSaved(editor);
        BookmarkEditorAction action = input switch
        {
            "select" => BookmarkEditorAction.Select(new BookmarkSelection(selection.Entry)),
            "search" => BookmarkEditorAction.Search(saved.Context.SearchText),
            "filter" => BookmarkEditorAction.Filter(saved.Context.Filter),
            _ => throw new InvalidOperationException("Unknown repeated browse input fixture."),
        };
        for (int iteration = 0; iteration < 2; iteration++)
        {
            _ = editor.Apply(action, catalog, BookmarkEditorTestValues.EmptyDefaults());
            BookmarksBrowsing after = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
            Assert.AreEqual(saved.Context, after.Context);
            Assert.AreEqual(selection, after.SavedSelection);
            Assert.IsNull(after.Problem);
        }
    }
}
