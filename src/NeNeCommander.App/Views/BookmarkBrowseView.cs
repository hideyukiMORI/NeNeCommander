using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using Windows.System;

namespace NeNeCommander.App.Views;

/// <summary>
/// Renders the bookmark manager's browse controls (search, category column, bookmark list and
/// count), forwards their native changes as typed bookmark actions, and keeps keyboard focus on the
/// list that held it when a render replaces its rows (ADR-0079).
/// </summary>
internal sealed class BookmarkBrowseView
{
    private readonly Grid _root;
    private readonly ResourceLoader _resources;
    private readonly Action<UserIntent> _forward;
    private readonly Func<bool> _isRendering;
    private readonly BookmarkFocusRestoration _focus;
    private BookmarkBrowsePresentation? _renderedBrowse;
    private readonly ListView _categories;
    private readonly ListView _rows;
    private readonly TextBlock _emptyContent;
    private readonly TextBlock _count;

    internal BookmarkBrowseView(Grid root, ResourceLoader resources, Action<UserIntent> forward, Func<bool> isRendering)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        ArgumentNullException.ThrowIfNull(isRendering);
        _root = root;
        _resources = resources;
        _forward = forward;
        _isRendering = isRendering;
        _focus = new BookmarkFocusRestoration(callback =>
            _ = root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => callback()));
        Search = Find<TextBox>("BookmarkSearch");
        _categories = Find<ListView>("BookmarkCategories");
        _rows = Find<ListView>("BookmarkRows");
        _emptyContent = Find<TextBlock>("BookmarkEmptyContent");
        _count = Find<TextBlock>("BookmarkCount");
        RowAutomationIdentity.Attach<BookmarkRow>(_rows, row => row.AutomationId, row => row.NameText);
        RowAutomationIdentity.Attach<BookmarkCategoryOption>(
            _categories,
            option => option.AutomationId,
            option => option.DisplayText);
        AttachHandlers();
    }

    /// <summary>Occurs when the category column or the bookmark list receives focus.</summary>
    internal event EventHandler<BookmarkManagerRegion>? RegionFocused;

    /// <summary>Gets the search field, whose text composition the window consults.</summary>
    internal TextBox Search { get; }

    /// <summary>Gets the bookmark row the list shows as selected.</summary>
    internal BookmarkRow? SelectedRow => _rows.SelectedItem as BookmarkRow;

    /// <summary>Gets the category option the column shows as selected.</summary>
    internal BookmarkCategoryOption? SelectedCategory => _categories.SelectedItem as BookmarkCategoryOption;

    /// <summary>Gets whether the search field holds focus.</summary>
    internal bool SearchHasFocus => _root.XamlRoot is XamlRoot xamlRoot &&
        ReferenceEquals(FocusManager.GetFocusedElement(xamlRoot), Search);

    internal void Render(BookmarkManagerPresentation presentation)
    {
        BookmarkBrowsePresentation browse = presentation.Browse;
        if (Search.Text != browse.SearchText)
        {
            Search.Text = browse.SearchText;
        }
        RenderList(_categories, browse.Categories, SelectedOption(browse), _renderedBrowse?.Categories);
        RenderList(_rows, browse.Rows, browse.SelectedRow, _renderedBrowse?.Rows);
        _emptyContent.Visibility = browse.EmptyContent == BookmarkEmptyContent.Hidden
            ? Visibility.Collapsed
            : Visibility.Visible;
        _emptyContent.Text = _resources.GetString(browse.EmptyContent.ResourceKey);
        _count.Text = string.Format(
            CultureInfo.CurrentCulture,
            _resources.GetString("BookmarkCountFormat"),
            browse.Rows.Count);
        bool editable = presentation.Details.IsBrowsing;
        Search.IsEnabled = editable;
        _categories.IsEnabled = editable;
        _rows.IsEnabled = editable;
        _renderedBrowse = browse;
        if (!editable)
        {
            _focus.Cancel();
        }
    }

    /// <summary>Moves the selection of one region by one step and keeps focus on that region.</summary>
    /// <param name="region">Region that holds focus.</param>
    /// <param name="step">One for the next item, minus one for the previous item.</param>
    internal void Move(BookmarkManagerRegion region, int step)
    {
        ListView list = ListOf(region);
        int next = BookmarkManagerKeyboard.Move(list.SelectedIndex, list.Items.Count, step);
        if (next >= 0 && next != list.SelectedIndex)
        {
            list.SelectedIndex = next;
        }
        Focus(region);
    }

    /// <summary>Moves keyboard focus to the selected item of one region once its rows are realized.</summary>
    /// <param name="region">Region that receives focus.</param>
    internal void Focus(BookmarkManagerRegion region)
    {
        FocusWhenRealized(ListOf(region));
    }

    internal void FocusSearch()
    {
        _focus.Cancel();
        _ = Search.Focus(FocusState.Keyboard);
        Search.SelectAll();
    }

    internal void NavigateSelected()
    {
        if (_rows.SelectedItem is BookmarkRow row)
        {
            _forward(UserIntent.NavigateBookmark(row.Selection));
        }
    }

    private void RenderList<TItem>(
        ListView list,
        IReadOnlyList<TItem> items,
        TItem? selected,
        IReadOnlyList<TItem>? rendered)
        where TItem : class
    {
        if (rendered is not null && rendered.SequenceEqual(items))
        {
            if (!Equals(list.SelectedItem, selected))
            {
                list.SelectedItem = selected;
            }
            return;
        }
        bool heldFocus = HoldsFocus(list);
        list.ItemsSource = items;
        list.SelectedItem = selected;
        if (heldFocus)
        {
            _focus.Restore(() => FocusList(list));
        }
    }

    private void FocusWhenRealized(ListView list)
    {
        _focus.Request(() => FocusList(list));
    }

    private static void FocusList(ListView list)
    {
        if (list.SelectedItem is object selected)
        {
            list.ScrollIntoView(selected);
        }
        bool focused = list.ContainerFromIndex(list.SelectedIndex) is Control container &&
            container.Focus(FocusState.Keyboard);
        if (!focused)
        {
            _ = list.Focus(FocusState.Keyboard);
        }
    }

    private bool HoldsFocus(ListView list)
    {
        DependencyObject? element = _root.XamlRoot is XamlRoot xamlRoot
            ? FocusManager.GetFocusedElement(xamlRoot) as DependencyObject
            : null;
        while (element is not null)
        {
            if (ReferenceEquals(element, list))
            {
                return true;
            }
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    private ListView ListOf(BookmarkManagerRegion region)
    {
        return region == BookmarkManagerRegion.Categories ? _categories : _rows;
    }

    private void AttachHandlers()
    {
        Search.TextChanged += OnSearchChanged;
        Search.GotFocus += (_, _) => _focus.Cancel();
        _categories.SelectionChanged += OnCategorySelectionChanged;
        _categories.GotFocus += (_, _) => OnRegionFocused(BookmarkManagerRegion.Categories);
        _rows.SelectionChanged += OnRowSelectionChanged;
        _rows.GotFocus += (_, _) => OnRegionFocused(BookmarkManagerRegion.Bookmarks);
        _rows.DoubleTapped += OnRowsDoubleTapped;
        _rows.KeyDown += OnRowsKeyDown;
    }

    private void OnRegionFocused(BookmarkManagerRegion region)
    {
        if (!_isRendering())
        {
            _focus.Cancel();
        }
        RegionFocused?.Invoke(this, region);
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (!_isRendering())
        {
            Submit(BookmarkEditorAction.Search(Search.Text));
        }
    }

    private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (!_isRendering() && _categories.SelectedItem is BookmarkCategoryOption option)
        {
            Submit(BookmarkEditorAction.Filter(option.Filter));
        }
    }

    private void OnRowSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (!_isRendering())
        {
            Submit(BookmarkEditorAction.Select((_rows.SelectedItem as BookmarkRow)?.Selection));
        }
    }

    private void OnRowsDoubleTapped(object sender, DoubleTappedRoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        NavigateSelected();
    }

    private void OnRowsKeyDown(object sender, KeyRoutedEventArgs args)
    {
        _ = sender;
        if (args.Key == VirtualKey.Enter)
        {
            NavigateSelected();
            args.Handled = true;
        }
    }

    private void Submit(BookmarkEditorAction action)
    {
        _forward(UserIntent.ManageBookmarks(action));
    }

    private static BookmarkCategoryOption? SelectedOption(BookmarkBrowsePresentation browse)
    {
        return browse.Categories.FirstOrDefault(category => category.IsSelected);
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _root.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Bookmark manager control '{name}' is missing.");
    }
}
