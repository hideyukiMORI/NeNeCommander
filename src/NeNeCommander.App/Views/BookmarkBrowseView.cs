using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using Windows.System;

namespace NeNeCommander.App.Views;

/// <summary>Renders the BookmarkBrowseView controls and forwards typed bookmark actions.</summary>
internal sealed class BookmarkBrowseView
{
    private readonly Grid _root;
    private readonly ResourceLoader _resources;
    private readonly Action<UserIntent> _forward;
    private readonly Func<bool> _isRendering;
    private BookmarkBrowsePresentation? _renderedBrowse;
    private readonly TextBox _search;
    private readonly ListView _categories;
    private readonly ListView _rows;
    private readonly TextBlock _emptyContent;
    private readonly Button _addCategory;
    private readonly Button _renameCategory;
    private readonly Button _deleteCategory;
    private readonly Button _move;
    private readonly Button _add;
    private readonly Button _edit;
    private readonly Button _delete;
    private readonly Button _close;

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
        _search = Find<TextBox>("BookmarkSearch");
        _categories = Find<ListView>("BookmarkCategories");
        _rows = Find<ListView>("BookmarkRows");
        _emptyContent = Find<TextBlock>("BookmarkEmptyContent");
        _addCategory = Find<Button>("BookmarkAddCategory");
        _renameCategory = Find<Button>("BookmarkRenameCategory");
        _deleteCategory = Find<Button>("BookmarkDeleteCategory");
        _move = Find<Button>("BookmarkMove");
        _add = Find<Button>("BookmarkAdd");
        _edit = Find<Button>("BookmarkEdit");
        _delete = Find<Button>("BookmarkDelete");
        _close = Find<Button>("BookmarkClose");
        AttachHandlers();
    }

    internal void Render(BookmarkManagerPresentation presentation)
    {
        BookmarkBrowsePresentation browse = presentation.Browse;
        if (_search.Text != browse.SearchText)
        {
            _search.Text = browse.SearchText;
        }
        RenderCategories(browse);
        RenderRows(browse);
        _emptyContent.Visibility = browse.EmptyContent == BookmarkEmptyContent.Hidden
            ? Visibility.Collapsed
            : Visibility.Visible;
        _emptyContent.Text = _resources.GetString(browse.EmptyContent.ResourceKey);
        RenderAvailability(presentation.Details, browse);
        _renderedBrowse = browse;
    }

    private void RenderCategories(BookmarkBrowsePresentation browse)
    {
        bool categoriesChanged = _renderedBrowse is null ||
            !_renderedBrowse.Categories.SequenceEqual(browse.Categories);
        if (categoriesChanged)
        {
            _categories.ItemsSource = browse.Categories;
        }
        BookmarkCategoryOption? selectedCategory = SelectedCategory(browse);
        if (categoriesChanged ||
            (_categories.SelectedItem as BookmarkCategoryOption)?.Filter != selectedCategory?.Filter)
        {
            _categories.SelectedItem = selectedCategory;
        }
    }

    private void RenderRows(BookmarkBrowsePresentation browse)
    {
        bool rowsChanged = _renderedBrowse is null ||
            !_renderedBrowse.Rows.SequenceEqual(browse.Rows);
        if (rowsChanged)
        {
            _rows.ItemsSource = browse.Rows;
        }
        if (rowsChanged ||
            (_rows.SelectedItem as BookmarkRow)?.Selection != browse.SelectedRow?.Selection)
        {
            _rows.SelectedItem = browse.SelectedRow;
        }
    }

    private void RenderAvailability(BookmarkEditorDetails details, BookmarkBrowsePresentation browse)
    {
        bool editable = details.IsBrowsing;
        _search.IsEnabled = editable;
        _categories.IsEnabled = editable;
        _rows.IsEnabled = editable;
        _addCategory.IsEnabled = editable;
        _add.IsEnabled = editable;
        _close.IsEnabled = editable;
        _move.IsEnabled = editable && browse.SelectedRow is not null;
        _edit.IsEnabled = editable && browse.SelectedRow is not null;
        _delete.IsEnabled = editable && browse.SelectedRow is not null;
        BookmarkCategoryOption? category = _categories.SelectedItem as BookmarkCategoryOption;
        _renameCategory.IsEnabled = editable && category?.Selection is not null;
        _deleteCategory.IsEnabled = editable && category?.Selection is not null;
    }

    private void AttachHandlers()
    {
        _search.TextChanged += OnSearchChanged;
        _categories.SelectionChanged += OnCategorySelectionChanged;
        _rows.SelectionChanged += OnRowSelectionChanged;
        _rows.DoubleTapped += OnRowsDoubleTapped;
        _rows.KeyDown += OnRowsKeyDown;
        _addCategory.Click += (_, _) => Submit(BookmarkEditorAction.BeginAddCategory);
        _renameCategory.Click += (_, _) => SubmitSelectedCategory(BookmarkEditorAction.BeginRenameCategory);
        _deleteCategory.Click += (_, _) => SubmitSelectedCategory(BookmarkEditorAction.BeginDeleteCategory);
        _move.Click += (_, _) => NavigateSelected();
        _add.Click += (_, _) => Submit(BookmarkEditorAction.BeginAddBookmark);
        _edit.Click += (_, _) => SubmitSelectedBookmark(BookmarkEditorAction.BeginEditBookmark);
        _delete.Click += (_, _) => SubmitSelectedBookmark(BookmarkEditorAction.DeleteBookmark);
        _close.Click += (_, _) => Submit(BookmarkEditorAction.Cancel);
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (!_isRendering())
        {
            Submit(BookmarkEditorAction.Search(_search.Text));
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

    internal void NavigateSelected()
    {
        if (_rows.SelectedItem is BookmarkRow row)
        {
            _forward(UserIntent.NavigateBookmark(row.Selection));
        }
    }

    private void SubmitSelectedBookmark(Func<BookmarkSelection, BookmarkEditorAction> action)
    {
        if (_rows.SelectedItem is BookmarkRow row)
        {
            Submit(action(row.Selection));
        }
    }

    private void SubmitSelectedCategory(Func<BookmarkCategorySelection, BookmarkEditorAction> action)
    {
        if (_categories.SelectedItem is BookmarkCategoryOption { Selection: not null } option)
        {
            Submit(action(option.Selection));
        }
    }

    private void Submit(BookmarkEditorAction action)
    {
        _forward(UserIntent.ManageBookmarks(action));
    }

    private static BookmarkCategoryOption? SelectedCategory(BookmarkBrowsePresentation browse)
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

    internal void FocusSearch()
    {
        _ = _search.Focus(FocusState.Programmatic);
    }
}
