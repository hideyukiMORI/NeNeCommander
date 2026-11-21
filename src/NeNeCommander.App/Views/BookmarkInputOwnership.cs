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
    private readonly Action _cancelPendingFocus;
    private readonly HashSet<TextBox> _fields = [];
    private readonly HashSet<TextBox> _composing = [];
    private readonly ComboBox _category;
    private readonly ComboBox _shortcut;

    internal BookmarkInputOwnership(Grid root, Action cancelPendingFocus)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(cancelPendingFocus);
        _root = root;
        _cancelPendingFocus = cancelPendingFocus;
        foreach (string name in new[] { "BookmarkSearch", "BookmarkName", "BookmarkPath", "BookmarkCategoryName" })
        {
            TextBox field = Find<TextBox>(name);
            _ = _fields.Add(field);
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

    internal void ObserveNativeInput()
    {
        if (_root.XamlRoot is XamlRoot xamlRoot &&
            FocusManager.GetFocusedElement(xamlRoot) is TextBox field && _fields.Contains(field))
        {
            _cancelPendingFocus();
        }
    }

    private void OnCompositionStarted(TextBox sender, TextCompositionStartedEventArgs args)
    {
        _ = args;
        _ = _composing.Add(sender);
        ObserveNativeInput();
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
