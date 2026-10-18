using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Bookmarks;

namespace NeNeCommander.App.Views;

/// <summary>Renders the BookmarkDetailsView controls and forwards typed bookmark actions.</summary>
internal sealed class BookmarkDetailsView
{
    private readonly Grid _root;
    private readonly ResourceLoader _resources;
    private readonly Action<UserIntent> _forward;
    private readonly Func<bool> _isRendering;
    private bool _stateChanged;
    private readonly Grid _browseBody;
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
    private readonly Button _save;
    private readonly Button _draftCancel;
    private readonly Button _categorySave;
    private readonly Button _categoryCancel;
    private readonly Button _confirmCategoryDelete;
    private readonly Button _cancelCategoryDelete;

    internal BookmarkDetailsView(Grid root, ResourceLoader resources, Action<UserIntent> forward, Func<bool> isRendering)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        ArgumentNullException.ThrowIfNull(isRendering);
        _root = root;
        _resources = resources;
        _forward = forward;
        _isRendering = isRendering;
        _browseBody = Find<Grid>("BookmarkBrowseBody");
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
        _save = Find<Button>("BookmarkSave");
        _draftCancel = Find<Button>("BookmarkDraftCancel");
        _categorySave = Find<Button>("BookmarkCategorySave");
        _categoryCancel = Find<Button>("BookmarkCategoryCancel");
        _confirmCategoryDelete = Find<Button>("BookmarkCategoryDeleteConfirm");
        _cancelCategoryDelete = Find<Button>("BookmarkCategoryDeleteCancel");
        AttachHandlers();
    }

    internal void Render(BookmarkEditorDetails details, Type? previousStateType, Type currentStateType)
    {
        ArgumentNullException.ThrowIfNull(currentStateType);
        _stateChanged = previousStateType != currentStateType;
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

    private void AttachHandlers()
    {
        _name.TextChanged += OnBookmarkDraftChanged;
        _path.TextChanged += OnBookmarkDraftChanged;
        _draftCategory.SelectionChanged += OnBookmarkDraftChanged;
        _draftShortcut.SelectionChanged += OnBookmarkDraftChanged;
        _categoryName.TextChanged += OnCategoryNameChanged;
        _save.Click += (_, _) => Submit(BookmarkEditorAction.Save);
        _draftCancel.Click += (_, _) => Submit(BookmarkEditorAction.Cancel);
        _categorySave.Click += (_, _) => Submit(BookmarkEditorAction.Save);
        _categoryCancel.Click += (_, _) => Submit(BookmarkEditorAction.Cancel);
        _confirmCategoryDelete.Click += (_, _) => Submit(BookmarkEditorAction.ConfirmDeleteCategory);
        _cancelCategoryDelete.Click += (_, _) => Submit(BookmarkEditorAction.Cancel);
    }

    private void OnBookmarkDraftChanged(object sender, object args)
    {
        _ = sender;
        _ = args;
        if (_isRendering() ||
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
        if (!_isRendering())
        {
            Submit(BookmarkEditorAction.UpdateCategory(_categoryName.Text));
        }
    }

    private void Submit(BookmarkEditorAction action)
    {
        _forward(UserIntent.ManageBookmarks(action));
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _root.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Bookmark manager control '{name}' is missing.");
    }

    internal bool Focus(BookmarkManagerFocusTarget target)
    {
        Control? control = target switch
        {
            BookmarkManagerFocusTarget value when value == BookmarkManagerFocusTarget.BookmarkName => _name,
            BookmarkManagerFocusTarget value when value == BookmarkManagerFocusTarget.CategoryName => _categoryName,
            BookmarkManagerFocusTarget value when value == BookmarkManagerFocusTarget.CancelCategoryDelete => _cancelCategoryDelete,
            _ => null,
        };
        if (control is null)
        {
            return false;
        }
        _ = control.Focus(FocusState.Programmatic);
        return true;
    }
}
