using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves bookmark navigation preserves the captured selection through failure and retry.</summary>
[TestClass]
public sealed class BookmarkEditorNavigationTests
{
    /// <summary>Proves pending navigation freezes Escape and failure retains the browse context for retry.</summary>
    [TestMethod]
    public void NavigationWhenPendingFreezesActionsAndFailureReturnsThroughTheClosedLayers()
    {
        BookmarkEntry entry = BookmarkEditorTestValues.Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([], [entry]);
        BookmarkSelection selection = new(entry);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        BookmarkRegistrationDefaults defaults = new(string.Empty, string.Empty);
        _ = editor.Apply(BookmarkEditorAction.Search("rep"), catalog, defaults);
        _ = editor.Apply(BookmarkEditorAction.Select(selection), catalog, defaults);

        Assert.IsTrue(editor.BeginNavigation(selection, catalog));
        _ = Assert.IsInstanceOfType<BookmarkEditorTransition.StateChanged>(
            editor.Apply(BookmarkEditorAction.Cancel, catalog, defaults));
        _ = Assert.IsInstanceOfType<BookmarkNavigationPending>(editor.Current);

        editor.FinishNavigationFailed(new PaneReadCancelled(entry.Path.Value));
        BookmarkNavigationFailed failed = Assert.IsInstanceOfType<BookmarkNavigationFailed>(editor.Current);
        Assert.AreEqual("rep", failed.ReturnContext.SearchText);
        Assert.AreEqual(selection, failed.Selection);

        _ = editor.Apply(BookmarkEditorAction.Cancel, catalog, defaults);
        BookmarksBrowsing browsing = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.AreEqual("rep", browsing.Context.SearchText);
        _ = Assert.IsInstanceOfType<BookmarkEditorTransition.CloseRequested>(
            editor.Apply(BookmarkEditorAction.Cancel, catalog, defaults));
    }

    /// <summary>Proves navigation rejects closed or stale selections and closes only after success.</summary>
    [TestMethod]
    public void NavigationWhenStartingRequiresOpenCurrentSelectionAndSuccessClosesEditor()
    {
        BookmarkEntry current = BookmarkEditorTestValues.Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([], [current]);
        BookmarkEditorSession editor = new();

        Assert.IsFalse(editor.BeginNavigation(new BookmarkSelection(current), catalog));
        Assert.AreSame(BookmarksEditorState.Closed, editor.Current);
        editor.Open();
        Assert.IsFalse(editor.BeginNavigation(
            new BookmarkSelection(BookmarkEditorTestValues.Entry("Repo", "C:\\old", null)),
            catalog));
        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Problem);
        Assert.IsTrue(editor.BeginNavigation(new BookmarkSelection(current), catalog));

        editor.FinishNavigationSucceeded();

        Assert.AreSame(BookmarksEditorState.Closed, editor.Current);
        editor.FinishNavigationSucceeded();
        editor.FinishNavigationFailed(new PaneReadCancelled(current.Path.Value));
        Assert.AreSame(BookmarksEditorState.Closed, editor.Current);
    }

    /// <summary>Proves Retry reuses the immutable selection retained by a failed navigation.</summary>
    [TestMethod]
    public void NavigationWhenRetryingFailureReturnsToPendingWithTheSameSelection()
    {
        BookmarkEntry entry = BookmarkEditorTestValues.Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([], [entry]);
        BookmarkSelection selection = new(entry);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        Assert.IsTrue(editor.BeginNavigation(selection, catalog));
        editor.FinishNavigationFailed(new PaneReadCancelled(entry.Path.Value));

        Assert.IsTrue(editor.BeginNavigation(selection, catalog));

        BookmarkNavigationPending pending =
            Assert.IsInstanceOfType<BookmarkNavigationPending>(editor.Current);
        Assert.AreSame(selection, pending.Selection);
    }

    /// <summary>Proves a failed navigation accepts only its retained complete selection.</summary>
    [TestMethod]
    public void NavigationWhenFailureReceivesAnotherCurrentSelectionKeepsTheFailureUnchanged()
    {
        BookmarkEntry retained = BookmarkEditorTestValues.Entry("Retained", "C:\\retained", null);
        BookmarkEntry other = BookmarkEditorTestValues.Entry("Other", "C:\\other", null);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([], [retained, other]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        Assert.IsTrue(editor.BeginNavigation(new BookmarkSelection(retained), catalog));
        editor.FinishNavigationFailed(new PaneReadCancelled(retained.Path.Value));
        BookmarkNavigationFailed failed =
            Assert.IsInstanceOfType<BookmarkNavigationFailed>(editor.Current);

        Assert.IsFalse(editor.BeginNavigation(new BookmarkSelection(other), catalog));

        Assert.AreSame(failed, editor.Current);
        Assert.IsTrue(editor.BeginNavigation(new BookmarkSelection(retained), catalog));
        BookmarkNavigationPending retry =
            Assert.IsInstanceOfType<BookmarkNavigationPending>(editor.Current);
        Assert.AreEqual("Retained", retry.Selection.Entry.Name.Value);
    }

    /// <summary>Proves a failure state cannot retain an idle or in-progress pane activity.</summary>
    [TestMethod]
    public void NavigationFailureWhenReasonIsNotACompletedRejectionRejectsTheReason()
    {
        BookmarkEntry entry = BookmarkEditorTestValues.Entry("Repo", "C:\\repo", null);
        BookmarkBrowseContext context = new(string.Empty, BookmarkCategoryFilter.All, null);

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            new BookmarkNavigationFailed(context, new BookmarkSelection(entry), PaneActivity.Idle));
    }
}
