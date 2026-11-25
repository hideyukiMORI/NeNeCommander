using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Paints the focused bookmark region's fixed-size heading, including an empty list.</summary>
internal sealed class BookmarkRegionFocusView
{
    private readonly Border _categories;
    private readonly Border _bookmarks;

    internal BookmarkRegionFocusView(Border categories, Border bookmarks)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(bookmarks);
        _categories = categories;
        _bookmarks = bookmarks;
    }

    internal void Render(BookmarkManagerRegion? region)
    {
        Paint(_categories, region == BookmarkManagerRegion.Categories ? PaneRowMark.FocusInActivePane : PaneRowMark.Unmarked);
        Paint(_bookmarks, region == BookmarkManagerRegion.Bookmarks ? PaneRowMark.FocusInActivePane : PaneRowMark.Unmarked);
    }

    private static void Paint(Border heading, PaneRowMark mark)
    {
        heading.BorderBrush = BrushOf(mark.MarkerBrushResourceKey);
        heading.Background = BrushOf(mark.SurfaceBrushResourceKey);
    }

    private static Brush BrushOf(string key)
    {
        return (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];
    }
}
