using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Bookmarks;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves BookmarkNavigationPresenter behavior and contracts.</summary>
[TestClass]
public sealed class BookmarkNavigationPresenterTests
{
    /// <summary>Proves pending navigation freezes input and failed navigation targets Retry.</summary>
    [TestMethod]
    public void PresentWhenNavigationVariesFreezesPendingAndFocusesFailedRetry()
    {
        BookmarkEntry entry = BookmarkPresenterTestContext.Entry("Offline", "\\\\server\\share", null, null);
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([], [entry]);
        BookmarkSelection selection = new(entry);
        BookmarkBrowseContext context = new("", BookmarkCategoryFilter.All, selection);
        BookmarksEditorState pending = BookmarkPresenterTestContext.Construct<BookmarkNavigationPending>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkSelection)],
            [context, selection]);
        BookmarksEditorState failed = BookmarkPresenterTestContext.Construct<BookmarkNavigationFailed>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkSelection), typeof(PaneActivity)],
            [context, selection, BookmarkPresenterTestContext.Cancelled(entry.Path.Value)]);

        BookmarkManagerPresentation pendingPresentation = BookmarkPresenterTestContext.Present(catalog, pending);
        BookmarkManagerPresentation failedPresentation = BookmarkPresenterTestContext.Present(catalog, failed);

        Assert.IsTrue(pendingPresentation.Details.IsNavigationPending);
        Assert.IsTrue(pendingPresentation.Details.IsInputFrozen);
        Assert.AreSame(selection, pendingPresentation.Details.NavigationSelection);
        Assert.AreEqual("BookmarkStatusNavigationPending", pendingPresentation.Details.StatusResourceKey);
        Assert.IsTrue(failedPresentation.Details.IsNavigationFailed);
        Assert.IsFalse(failedPresentation.Details.IsInputFrozen);
        Assert.AreSame(selection, failedPresentation.Details.NavigationSelection);
        Assert.AreSame(
            BookmarkManagerFocusTarget.RetryNavigation,
            failedPresentation.Details.InitialFocus);
        Assert.AreEqual(
            "BookmarkStatusNavigationCancelled",
            failedPresentation.Details.StatusResourceKey);
    }

    /// <summary>Proves every reachable pane failure uses the shared normalized status mapping.</summary>
    [TestMethod]
    public void PresentWhenNavigationFailsReportsItsNormalizedReasonWithoutRawDetails()
    {
        BookmarkEntry entry = BookmarkPresenterTestContext.Entry("Offline", "C:\\offline", null, null);
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([], [entry]);
        BookmarkSelection selection = new(entry);
        BookmarkBrowseContext context = new("", BookmarkCategoryFilter.All, selection);
        (PaneActivity Reason, string ResourceKey)[] cases =
        [
            (BookmarkPresenterTestContext.Failed(entry.Path.Value, FileOperationFailureKind.AccessDenied),
                "BookmarkStatusNavigationAccessDenied"),
            (BookmarkPresenterTestContext.Failed(entry.Path.Value, FileOperationFailureKind.NotFound),
                "BookmarkStatusNavigationNotFound"),
            (BookmarkPresenterTestContext.Failed(entry.Path.Value, FileOperationFailureKind.ProviderUnavailable),
                "BookmarkStatusNavigationProviderUnavailable"),
            (BookmarkPresenterTestContext.Cancelled(entry.Path.Value), "BookmarkStatusNavigationCancelled"),
        ];

        foreach ((PaneActivity reason, string resourceKey) in cases)
        {
            BookmarksEditorState state = BookmarkPresenterTestContext.Construct<BookmarkNavigationFailed>(
                [typeof(BookmarkBrowseContext), typeof(BookmarkSelection), typeof(PaneActivity)],
                [context, selection, reason]);

            BookmarkManagerPresentation presentation = BookmarkPresenterTestContext.Present(catalog, state);

            Assert.AreEqual(resourceKey, presentation.Details.StatusResourceKey);
            Assert.AreSame(selection, presentation.Details.NavigationSelection);
            Assert.AreSame(
                BookmarkManagerFocusTarget.RetryNavigation,
                presentation.Details.InitialFocus);
        }
    }
}
