using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace NeNeCommander.App.Views;

/// <summary>Keeps bookmark text composition and open selectors in their native input controls.</summary>
internal sealed class BookmarkInputOwnership
{
    private readonly Grid _root;
    private readonly HashSet<TextBox> _composing = [];
    private readonly ComboBox _category;
    private readonly ComboBox _shortcut;

    internal BookmarkInputOwnership(Grid root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
        foreach (string name in new[] { "BookmarkSearch", "BookmarkName", "BookmarkPath", "BookmarkCategoryName" })
        {
            TextBox field = Find<TextBox>(name);
            field.TextCompositionStarted += OnCompositionStarted;
            field.TextCompositionEnded += OnCompositionEnded;
            field.LostFocus += (_, _) => _ = _composing.Remove(field);
        }
        _category = Find<ComboBox>("BookmarkDraftCategory");
        _shortcut = Find<ComboBox>("BookmarkDraftShortcut");
    }

    internal bool IsComposing => _root.XamlRoot is XamlRoot xamlRoot &&
        FocusManager.GetFocusedElement(xamlRoot) is TextBox textBox && _composing.Contains(textBox);

    internal bool HasOpenSelector => _category.IsDropDownOpen || _shortcut.IsDropDownOpen;

    private void OnCompositionStarted(TextBox sender, TextCompositionStartedEventArgs args)
    {
        _ = args;
        _ = _composing.Add(sender);
    }

    private void OnCompositionEnded(TextBox sender, TextCompositionEndedEventArgs args)
    {
        _ = args;
        _ = _composing.Remove(sender);
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _root.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Bookmark input control '{name}' is missing.");
    }
}
