using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using NeNeCommander.Presentation.WinUI.Bookmarks;

namespace NeNeCommander.App.Views;

/// <summary>
/// Projects one interactive bookmark key band. Inert, raw-accessibility copies of the same content
/// template reserve the largest wrapped extent of all three focus contexts at the current width.
/// Only the interactive band changes with focus; no hidden button or alternate command route exists.
/// </summary>
internal sealed class BookmarkKeyHintView
{
    private readonly ItemsControl _hints;
    private readonly IReadOnlyList<BookmarkManagerKeyHint> _categories = BookmarkManagerKeyHintPresenter.Present(BookmarkManagerRegion.Categories);
    private readonly IReadOnlyList<BookmarkManagerKeyHint> _bookmarks = BookmarkManagerKeyHintPresenter.Present(BookmarkManagerRegion.Bookmarks);
    private readonly IReadOnlyList<BookmarkManagerKeyHint> _search = BookmarkManagerKeyHintPresenter.PresentSearch();

    internal BookmarkKeyHintView(Grid root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _hints = Find(root, "BookmarkBrowseFooter");
        Find(root, "BookmarkCategoryHintMeasure").ItemsSource = _categories;
        Find(root, "BookmarkRowHintMeasure").ItemsSource = _bookmarks;
        Find(root, "BookmarkSearchHintMeasure").ItemsSource = _search;
    }

    internal void Render(BookmarkManagerRegion region)
    {
        _hints.ItemsSource = region == BookmarkManagerRegion.Categories ? _categories : _bookmarks;
    }

    internal void RenderSearch()
    {
        _hints.ItemsSource = _search;
    }

    private static ItemsControl Find(Grid root, string name)
    {
        return root.FindName(name) is ItemsControl control
            ? control
            : throw new InvalidOperationException($"Bookmark key-hint control '{name}' is missing.");
    }
}
