using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves category mutations preserve membership and reject stale selections.</summary>
[TestClass]
public sealed class BookmarkEditorCategoryTests
{
    /// <summary>Proves a category deletion collision changes neither the catalog nor confirmation context.</summary>
    [TestMethod]
    public void ApplyWhenCategoryDeletionWouldCollideKeepsEveryRegistration()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog(
            [work],
            [BookmarkEditorTestValues.Entry("Repo", "C:\\one", null), BookmarkEditorTestValues.Entry("repo", "C:\\two", work)]);
        BookmarkCategorySelection selection = catalog.Select(work) ??
            throw new InvalidOperationException("The category fixture must be selectable.");
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        BookmarkRegistrationDefaults defaults = new(string.Empty, string.Empty);

        _ = editor.Apply(BookmarkEditorAction.BeginDeleteCategory(selection), catalog, defaults);
        BookmarkCategoryDeleteConfirmation confirmation =
            Assert.IsInstanceOfType<BookmarkCategoryDeleteConfirmation>(editor.Current);
        Assert.HasCount(1, confirmation.Selection.Entries);
        BookmarkEditorTransition transition = editor.Apply(
            BookmarkEditorAction.ConfirmDeleteCategory,
            catalog,
            defaults);

        _ = Assert.IsInstanceOfType<BookmarkEditorTransition.StateChanged>(transition);
        BookmarksBrowsing rejected = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.AreSame(BookmarkEditorProblem.CategoryDeleteCollision, rejected.Problem);
        Assert.HasCount(1, catalog.Categories);
        Assert.HasCount(2, catalog.Bookmarks);
    }

    /// <summary>Proves a category draft rejects invalid text before accepting one complete addition.</summary>
    [TestMethod]
    public void ApplyWhenAddingCategoryRetainsInvalidTextThenAppendsTheAcceptedName()
    {
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = editor.Apply(BookmarkEditorAction.BeginAddCategory, BookmarkCatalog.Empty, BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(
            BookmarkEditorAction.UpdateCategory(""),
            BookmarkCatalog.Empty,
            BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.Save, BookmarkCatalog.Empty, BookmarkEditorTestValues.EmptyDefaults());

        BookmarkCategoryDrafting rejected =
            Assert.IsInstanceOfType<BookmarkCategoryDrafting>(editor.Current);
        Assert.AreEqual(string.Empty, rejected.Name);
        Assert.AreSame(BookmarkEditorProblem.InvalidName, rejected.Problem);

        _ = editor.Apply(
            BookmarkEditorAction.UpdateCategory("Work"),
            BookmarkCatalog.Empty,
            BookmarkEditorTestValues.EmptyDefaults());
        BookmarkEditorTransition.CatalogChanged changed =
            Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
                editor.Apply(BookmarkEditorAction.Save, BookmarkCatalog.Empty, BookmarkEditorTestValues.EmptyDefaults()));

        Assert.HasCount(1, changed.Catalog.Categories);
        Assert.AreEqual("Work", changed.Catalog.Categories[0].Value);
    }

    /// <summary>Proves category rename updates references and rebinds the active category filter.</summary>
    [TestMethod]
    public void ApplyWhenRenamingCurrentCategoryRebindsFilterAndEveryReference()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([work], [BookmarkEditorTestValues.Entry("Repo", "C:\\repo", work)]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(work)),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        BookmarkCategorySelection selection = BookmarkEditorTestValues.RequireCategory(catalog, work);
        _ = editor.Apply(
            BookmarkEditorAction.BeginRenameCategory(selection),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(
            BookmarkEditorAction.UpdateCategory("Projects"),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());

        BookmarkEditorTransition.CatalogChanged changed =
            Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
                editor.Apply(BookmarkEditorAction.Save, catalog, BookmarkEditorTestValues.EmptyDefaults()));

        Assert.AreEqual("Projects", changed.Catalog.Categories[0].Value);
        Assert.AreEqual("Projects", changed.Catalog.Bookmarks[0].Category?.Value);
        BookmarkUserCategoryFilter filter = Assert.IsInstanceOfType<BookmarkUserCategoryFilter>(
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Filter);
        Assert.AreEqual("Projects", filter.Category.Value);
    }

    /// <summary>Proves duplicate and capacity category failures keep their respective drafts open.</summary>
    [TestMethod]
    public void ApplyWhenCategoryMutationConflictsReportsDuplicateOrCapacityWithoutChange()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCategoryName other = BookmarkEditorTestValues.Category("Other");
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([work, other], []);
        BookmarkEditorSession renameEditor = BookmarkEditorTestValues.OpenEditor();
        _ = renameEditor.Apply(
            BookmarkEditorAction.BeginRenameCategory(BookmarkEditorTestValues.RequireCategory(catalog, work)),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        _ = renameEditor.Apply(
            BookmarkEditorAction.UpdateCategory("other"),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        _ = renameEditor.Apply(BookmarkEditorAction.Save, catalog, BookmarkEditorTestValues.EmptyDefaults());
        Assert.AreSame(
            BookmarkEditorProblem.DuplicateCategory,
            Assert.IsInstanceOfType<BookmarkCategoryDrafting>(renameEditor.Current).Problem);

        BookmarkCatalog full = BookmarkEditorTestValues.Catalog(BookmarkEditorTestValues.MaximumCategories(), []);
        BookmarkEditorSession addEditor = BookmarkEditorTestValues.OpenEditor();
        _ = addEditor.Apply(BookmarkEditorAction.BeginAddCategory, full, BookmarkEditorTestValues.EmptyDefaults());
        _ = addEditor.Apply(
            BookmarkEditorAction.UpdateCategory("Overflow"),
            full,
            BookmarkEditorTestValues.EmptyDefaults());
        _ = addEditor.Apply(BookmarkEditorAction.Save, full, BookmarkEditorTestValues.EmptyDefaults());
        Assert.AreSame(
            BookmarkEditorProblem.CategoryLimit,
            Assert.IsInstanceOfType<BookmarkCategoryDrafting>(addEditor.Current).Problem);
    }

    /// <summary>Proves successful category deletion moves entries and the active filter together.</summary>
    [TestMethod]
    public void ApplyWhenDeletingCurrentCategoryMovesEntriesToUncategorizedAtomically()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([work], [BookmarkEditorTestValues.Entry("Repo", "C:\\repo", work)]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(work)),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        BookmarkCategorySelection selection = BookmarkEditorTestValues.RequireCategory(catalog, work);
        _ = editor.Apply(
            BookmarkEditorAction.BeginDeleteCategory(selection),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());

        BookmarkEditorTransition.CatalogChanged changed =
            Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
                editor.Apply(
                    BookmarkEditorAction.ConfirmDeleteCategory,
                    catalog,
                    BookmarkEditorTestValues.EmptyDefaults()));

        Assert.IsEmpty(changed.Catalog.Categories);
        Assert.IsNull(changed.Catalog.Bookmarks[0].Category);
        _ = Assert.IsInstanceOfType<BookmarkUncategorizedCategoryFilter>(
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Filter);
    }

    /// <summary>Proves category confirmation is invalidated by catalog changes before confirmation.</summary>
    [TestMethod]
    public void ApplyWhenConfirmedCategorySelectionBecameStaleRejectsTheWholeDeletion()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkEntry displayed = BookmarkEditorTestValues.Entry("Repo", "C:\\old", work);
        BookmarkCatalog initial = BookmarkEditorTestValues.Catalog([work], [displayed]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = editor.Apply(
            BookmarkEditorAction.BeginDeleteCategory(BookmarkEditorTestValues.RequireCategory(initial, work)),
            initial,
            BookmarkEditorTestValues.EmptyDefaults());
        BookmarkCatalog replaced = BookmarkEditorTestValues.Catalog([work], [BookmarkEditorTestValues.Entry("Repo", "C:\\new", work)]);

        _ = editor.Apply(
            BookmarkEditorAction.ConfirmDeleteCategory,
            replaced,
            BookmarkEditorTestValues.EmptyDefaults());

        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Problem);
        Assert.AreEqual("Work", replaced.Categories[0].Value);
        Assert.AreEqual("C:\\new", replaced.Bookmarks[0].Path.Value.CanonicalText);
    }

    /// <summary>Proves rename begins only from the unchanged complete category selection.</summary>
    [TestMethod]
    public void ApplyWhenRenameCategorySelectionIsStaleRemainsBrowsing()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCatalog displayed = BookmarkEditorTestValues.Catalog([work], [BookmarkEditorTestValues.Entry("Repo", "C:\\old", work)]);
        BookmarkCategorySelection selection = BookmarkEditorTestValues.RequireCategory(displayed, work);
        BookmarkCatalog current = BookmarkEditorTestValues.Catalog([work], [BookmarkEditorTestValues.Entry("Repo", "C:\\new", work)]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();

        _ = editor.Apply(
            BookmarkEditorAction.BeginRenameCategory(selection),
            current,
            BookmarkEditorTestValues.EmptyDefaults());

        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Problem);
    }

    /// <summary>Proves category deletion confirmation opens only for unchanged category metadata.</summary>
    [TestMethod]
    public void ApplyWhenDeleteCategorySelectionIsStaleDoesNotOpenConfirmation()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCatalog displayed = BookmarkEditorTestValues.Catalog([work], [BookmarkEditorTestValues.Entry("Repo", "C:\\old", work)]);
        BookmarkCategorySelection selection = BookmarkEditorTestValues.RequireCategory(displayed, work);
        BookmarkCatalog current = BookmarkEditorTestValues.Catalog([work], [BookmarkEditorTestValues.Entry("Repo", "C:\\new", work)]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();

        _ = editor.Apply(
            BookmarkEditorAction.BeginDeleteCategory(selection),
            current,
            BookmarkEditorTestValues.EmptyDefaults());

        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Problem);
    }
}
