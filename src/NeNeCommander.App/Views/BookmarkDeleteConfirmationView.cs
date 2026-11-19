using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Bookmarks;

namespace NeNeCommander.App.Views;

/// <summary>
/// Renders the bookmark deletion question inside the manager panel and forwards its two buttons as
/// the existing typed actions (ADR-0080). It owns no editor state.
/// </summary>
internal sealed class BookmarkDeleteConfirmationView
{
    private readonly ScrollViewer _body;
    private readonly StackPanel _footer;
    private readonly TextBlock _name;
    private readonly TextBlock _path;

    internal BookmarkDeleteConfirmationView(Grid root, Action<UserIntent> forward)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(forward);
        _body = Find<ScrollViewer>(root, "BookmarkDeleteBody");
        _footer = Find<StackPanel>(root, "BookmarkDeleteFooter");
        _name = Find<TextBlock>(root, "BookmarkDeleteName");
        _path = Find<TextBlock>(root, "BookmarkDeletePath");
        Cancel = Find<Button>(root, "BookmarkDeleteCancel");
        Find<Button>(root, "BookmarkDeleteConfirm").Click += (_, _) =>
            forward(UserIntent.ManageBookmarks(BookmarkEditorAction.ConfirmDeleteBookmark));
        Cancel.Click += (_, _) => forward(UserIntent.ManageBookmarks(BookmarkEditorAction.Cancel));
    }

    /// <summary>Gets the default-safe Cancel button that receives focus when the question opens.</summary>
    internal Button Cancel { get; }

    /// <summary>Shows the question with its target while one awaits confirmation, otherwise hides it.</summary>
    /// <param name="details">Rendered manager substate.</param>
    internal void Render(BookmarkEditorDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        Visibility visibility = details.BookmarkDeleteSelection is null ? Visibility.Collapsed : Visibility.Visible;
        _body.Visibility = visibility;
        _footer.Visibility = visibility;
        if (details.BookmarkDeleteSelection is BookmarkSelection deletion)
        {
            _name.Text = deletion.Entry.Name.Value;
            _path.Text = deletion.Entry.Path.Value.CanonicalText;
        }
    }

    private static T Find<T>(Grid root, string name)
        where T : DependencyObject
    {
        return root.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Bookmark manager control '{name}' is missing.");
    }
}
