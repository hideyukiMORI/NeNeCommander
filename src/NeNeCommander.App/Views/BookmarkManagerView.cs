using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using Windows.System;

namespace NeNeCommander.App.Views;

/// <summary>Renders the bookmark snapshot and translates native controls into typed intents.</summary>
internal sealed class BookmarkManagerView
{
    private readonly Action<UserIntent> _forward;
    private readonly ResourceLoader _resources;
    private readonly Grid _root;
    private readonly TextBox _search;
    private readonly TextBlock _status;
    private readonly TextBlock _saveStatus;
    private readonly Border _warning;
    private readonly TextBlock _warningText;
    private readonly Grid _browseBody;
    private readonly ListView _categories;
    private readonly ListView _rows;
    private readonly Button _addCategory;
    private readonly Button _renameCategory;
    private readonly Button _deleteCategory;
    private readonly ScrollViewer _draftBody;
    private readonly TextBlock _draftTitle;
    private readonly TextBox _name;
    private readonly TextBox _path;
    private readonly ComboBox _draftCategory;
    private readonly ComboBox _draftShortcut;
    private readonly StackPanel _categoryDraftBody;
    private readonly TextBlock _categoryDraftTitle;
    private readonly TextBox _categoryName;
    private readonly StackPanel _categoryDeleteBody;
    private readonly TextBlock _deleteCategoryName;
    private readonly TextBlock _deleteCount;
    private readonly TextBlock _deleteDestination;
    private readonly StackPanel _browseFooter;
    private readonly StackPanel _draftFooter;
    private readonly StackPanel _categoryDraftFooter;
    private readonly StackPanel _categoryDeleteFooter;
    private readonly StackPanel _navigationFailedFooter;
    private readonly Button _move;
    private readonly Button _add;
    private readonly Button _edit;
    private readonly Button _delete;
    private readonly Button _close;
    private readonly Button _save;
    private readonly Button _draftCancel;
    private readonly Button _categorySave;
    private readonly Button _categoryCancel;
    private readonly Button _confirmCategoryDelete;
    private readonly Button _cancelCategoryDelete;
    private readonly Button _retryNavigation;
    private readonly Button _cancelNavigation;
    private Type? _renderedStateType;
    private BookmarkBrowsePresentation? _renderedBrowse;
    private bool _rendering;
    private bool _stateChanged;

    internal BookmarkManagerView(Grid root, ResourceLoader resources, Action<UserIntent> forward)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        _root = root;
        _resources = resources;
        _forward = forward;
        _search = Find<TextBox>("BookmarkSearch");
        _status = Find<TextBlock>("BookmarkStatus");
        _saveStatus = Find<TextBlock>("BookmarkSaveStatus");
        _warning = Find<Border>("BookmarkWarning");
        _warningText = Find<TextBlock>("BookmarkWarningText");
        _browseBody = Find<Grid>("BookmarkBrowseBody");
        _categories = Find<ListView>("BookmarkCategories");
        _rows = Find<ListView>("BookmarkRows");
        _addCategory = Find<Button>("BookmarkAddCategory");
        _renameCategory = Find<Button>("BookmarkRenameCategory");
        _deleteCategory = Find<Button>("BookmarkDeleteCategory");
        _draftBody = Find<ScrollViewer>("BookmarkDraftBody");
        _draftTitle = Find<TextBlock>("BookmarkDraftTitle");
        _name = Find<TextBox>("BookmarkName");
        _path = Find<TextBox>("BookmarkPath");
        _draftCategory = Find<ComboBox>("BookmarkDraftCategory");
        _draftShortcut = Find<ComboBox>("BookmarkDraftShortcut");
        _categoryDraftBody = Find<StackPanel>("BookmarkCategoryDraftBody");
        _categoryDraftTitle = Find<TextBlock>("BookmarkCategoryDraftTitle");
        _categoryName = Find<TextBox>("BookmarkCategoryName");
        _categoryDeleteBody = Find<StackPanel>("BookmarkCategoryDeleteBody");
        _deleteCategoryName = Find<TextBlock>("BookmarkCategoryDeleteCategory");
        _deleteCount = Find<TextBlock>("BookmarkCategoryDeleteCount");
        _deleteDestination = Find<TextBlock>("BookmarkCategoryDeleteDestination");
        _browseFooter = Find<StackPanel>("BookmarkBrowseFooter");
        _draftFooter = Find<StackPanel>("BookmarkDraftFooter");
        _categoryDraftFooter = Find<StackPanel>("BookmarkCategoryDraftFooter");
        _categoryDeleteFooter = Find<StackPanel>("BookmarkCategoryDeleteFooter");
        _navigationFailedFooter = Find<StackPanel>("BookmarkNavigationFailedFooter");
        _move = Find<Button>("BookmarkMove");
        _add = Find<Button>("BookmarkAdd");
        _edit = Find<Button>("BookmarkEdit");
        _delete = Find<Button>("BookmarkDelete");
        _close = Find<Button>("BookmarkClose");
        _save = Find<Button>("BookmarkSave");
        _draftCancel = Find<Button>("BookmarkDraftCancel");
        _categorySave = Find<Button>("BookmarkCategorySave");
        _categoryCancel = Find<Button>("BookmarkCategoryCancel");
        _confirmCategoryDelete = Find<Button>("BookmarkCategoryDeleteConfirm");
        _cancelCategoryDelete = Find<Button>("BookmarkCategoryDeleteCancel");
        _retryNavigation = Find<Button>("BookmarkNavigationRetry");
        _cancelNavigation = Find<Button>("BookmarkNavigationCancel");
        AttachHandlers();
    }

    internal bool IsOpen => _root.Visibility == Visibility.Visible;

    internal void Render(SettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        BookmarkManagerPresentation presentation = BookmarkManagerPresenter.Present(
            snapshot,
            _resources.GetString("BookmarkAllCategories"),
            _resources.GetString("BookmarkUncategorized"));
        _stateChanged = _renderedStateType != snapshot.BookmarksEditor.GetType();
        _rendering = true;
        _root.Visibility = presentation.IsOpen ? Visibility.Visible : Visibility.Collapsed;
        RenderBrowse(presentation);
        RenderDetails(presentation);
        RenderPersistence(presentation.Persistence);
        _rendering = false;
        if (presentation.IsOpen && _stateChanged)
        {
            Focus(presentation.Details.InitialFocus);
        }
        _renderedStateType = snapshot.BookmarksEditor.GetType();
    }

    private void RenderBrowse(BookmarkManagerPresentation presentation)
    {
        BookmarkBrowsePresentation browse = presentation.Browse;
        if (_search.Text != browse.SearchText)
        {
            _search.Text = browse.SearchText;
        }
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
        bool editable = presentation.Details.IsBrowsing;
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
        _renderedBrowse = browse;
    }

    private void RenderDetails(BookmarkManagerPresentation presentation)
    {
        BookmarkEditorDetails details = presentation.Details;
        _status.Text = _resources.GetString(details.StatusResourceKey);
        _browseBody.Visibility = details.IsBookmarkDrafting ||
            details.IsCategoryDrafting ||
            details.IsCategoryDeleteConfirmation
            ? Visibility.Collapsed
            : Visibility.Visible;
        _draftBody.Visibility = details.IsBookmarkDrafting ? Visibility.Visible : Visibility.Collapsed;
        _categoryDraftBody.Visibility = details.IsCategoryDrafting ? Visibility.Visible : Visibility.Collapsed;
        _categoryDeleteBody.Visibility = details.IsCategoryDeleteConfirmation
            ? Visibility.Visible
            : Visibility.Collapsed;
        _browseFooter.Visibility = details.IsBrowsing ? Visibility.Visible : Visibility.Collapsed;
        _draftFooter.Visibility = details.IsBookmarkDrafting ? Visibility.Visible : Visibility.Collapsed;
        _categoryDraftFooter.Visibility = details.IsCategoryDrafting ? Visibility.Visible : Visibility.Collapsed;
        _categoryDeleteFooter.Visibility = details.IsCategoryDeleteConfirmation
            ? Visibility.Visible
            : Visibility.Collapsed;
        _navigationFailedFooter.Visibility = details.IsNavigationFailed
            ? Visibility.Visible
            : Visibility.Collapsed;
        RenderBookmarkDraft(details);
        RenderCategoryState(details);
    }

    private void RenderBookmarkDraft(BookmarkEditorDetails details)
    {
        BookmarkDraft? draft = details.BookmarkDraft;
        if (draft is null)
        {
            return;
        }
        _draftTitle.Text = _resources.GetString(details.IsAddingBookmark
            ? "BookmarkDraftAddTitle"
            : "BookmarkDraftEditTitle");
        if (_name.Text != draft.Name)
        {
            _name.Text = draft.Name;
        }
        if (_path.Text != draft.Path)
        {
            _path.Text = draft.Path;
        }
        if (_stateChanged || _draftCategory.ItemsSource is null)
        {
            _draftCategory.ItemsSource = details.DraftCategories;
            _draftCategory.SelectedItem = details.SelectedDraftCategory;
            _draftShortcut.ItemsSource = details.Shortcuts;
            _draftShortcut.SelectedItem = details.SelectedShortcut;
        }
    }

    private void RenderCategoryState(BookmarkEditorDetails details)
    {
        if (details.IsCategoryDrafting)
        {
            _categoryDraftTitle.Text = _resources.GetString(details.IsAddingCategory
                ? "BookmarkCategoryAddTitle"
                : "BookmarkCategoryRenameTitle");
            if (_stateChanged || _categoryName.Text != details.CategoryDraftName)
            {
                _categoryName.Text = details.CategoryDraftName;
            }
        }
        if (details.IsCategoryDeleteConfirmation && details.CategorySelection is not null)
        {
            _deleteCategoryName.Text = details.CategorySelection.Category.Value;
            _deleteCount.Text = details.CategoryDeleteCount.ToString(System.Globalization.CultureInfo.CurrentCulture);
            _deleteDestination.Text = _resources.GetString("BookmarkUncategorized");
        }
    }

    private void RenderPersistence(BookmarkPersistencePresentation persistence)
    {
        _saveStatus.Text = _resources.GetString(persistence.SaveStatus.ResourceKey);
        if (persistence.Warning.IsVisible)
        {
            _warning.Visibility = Visibility.Visible;
            _warningText.Text = _resources.GetString(persistence.Warning.ResourceKey);
            return;
        }
        _warning.Visibility = Visibility.Collapsed;
        _warningText.Text = string.Empty;
    }

    private void AttachHandlers()
    {
        _search.TextChanged += OnSearchChanged;
        _categories.SelectionChanged += OnCategorySelectionChanged;
        _rows.SelectionChanged += OnRowSelectionChanged;
        _rows.DoubleTapped += OnRowsDoubleTapped;
        _rows.KeyDown += OnRowsKeyDown;
        _name.TextChanged += OnBookmarkDraftChanged;
        _path.TextChanged += OnBookmarkDraftChanged;
        _draftCategory.SelectionChanged += OnBookmarkDraftChanged;
        _draftShortcut.SelectionChanged += OnBookmarkDraftChanged;
        _categoryName.TextChanged += OnCategoryNameChanged;
        _addCategory.Click += (_, _) => Submit(BookmarkEditorAction.BeginAddCategory);
        _renameCategory.Click += (_, _) => SubmitSelectedCategory(BookmarkEditorAction.BeginRenameCategory);
        _deleteCategory.Click += (_, _) => SubmitSelectedCategory(BookmarkEditorAction.BeginDeleteCategory);
        _move.Click += (_, _) => NavigateSelected();
        _add.Click += (_, _) => Submit(BookmarkEditorAction.BeginAddBookmark);
        _edit.Click += (_, _) => SubmitSelectedBookmark(BookmarkEditorAction.BeginEditBookmark);
        _delete.Click += (_, _) => SubmitSelectedBookmark(BookmarkEditorAction.DeleteBookmark);
        _close.Click += (_, _) => Submit(BookmarkEditorAction.Cancel);
        _save.Click += (_, _) => Submit(BookmarkEditorAction.Save);
        _draftCancel.Click += (_, _) => Submit(BookmarkEditorAction.Cancel);
        _categorySave.Click += (_, _) => Submit(BookmarkEditorAction.Save);
        _categoryCancel.Click += (_, _) => Submit(BookmarkEditorAction.Cancel);
        _confirmCategoryDelete.Click += (_, _) => Submit(BookmarkEditorAction.ConfirmDeleteCategory);
        _cancelCategoryDelete.Click += (_, _) => Submit(BookmarkEditorAction.Cancel);
        _retryNavigation.Click += (_, _) => NavigateSelected();
        _cancelNavigation.Click += (_, _) => Submit(BookmarkEditorAction.Cancel);
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (!_rendering)
        {
            Submit(BookmarkEditorAction.Search(_search.Text));
        }
    }

    private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (!_rendering && _categories.SelectedItem is BookmarkCategoryOption option)
        {
            Submit(BookmarkEditorAction.Filter(option.Filter));
        }
    }

    private void OnRowSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (!_rendering)
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

    private void OnBookmarkDraftChanged(object sender, object args)
    {
        _ = sender;
        _ = args;
        if (_rendering ||
            _draftCategory.SelectedItem is not BookmarkCategoryOption category ||
            _draftShortcut.SelectedItem is not BookmarkShortcutOption shortcut)
        {
            return;
        }
        Submit(BookmarkEditorAction.UpdateBookmark(new BookmarkDraft(
            _name.Text,
            _path.Text,
            category.Filter,
            shortcut.Slot)));
    }

    private void OnCategoryNameChanged(object sender, TextChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (!_rendering)
        {
            Submit(BookmarkEditorAction.UpdateCategory(_categoryName.Text));
        }
    }

    private void NavigateSelected()
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

    private void Focus(BookmarkManagerFocusTarget target)
    {
        Control control = target == BookmarkManagerFocusTarget.BookmarkName
            ? _name
            : target == BookmarkManagerFocusTarget.CategoryName
                ? _categoryName
                : target == BookmarkManagerFocusTarget.CancelCategoryDelete
                    ? _cancelCategoryDelete
                    : target == BookmarkManagerFocusTarget.RetryNavigation
                        ? _retryNavigation
                        : _search;
        _ = control.Focus(FocusState.Programmatic);
    }

    private static BookmarkCategoryOption? SelectedCategory(BookmarkBrowsePresentation browse)
    {
        foreach (BookmarkCategoryOption category in browse.Categories)
        {
            if (category.IsSelected)
            {
                return category;
            }
        }
        return null;
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _root.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Bookmark manager control '{name}' is missing.");
    }
}
