using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.App.Views;

/// <summary>
/// Applies the settings editor's mapped keys to its native controls (ADR-0077). Movement only moves
/// focus through the items in display order; activation toggles the check box, selects the focused
/// scheme or closes, and every change reaches the session through the existing control handlers.
/// </summary>
internal sealed class SettingsKeyboardView
{
    private readonly CheckBox _showHidden;
    private readonly ItemsControl _schemes;
    private readonly Button _close;
    private readonly Func<XamlRoot> _root;

    internal SettingsKeyboardView(CheckBox showHidden, ItemsControl schemes, Button close, Func<XamlRoot> root)
    {
        ArgumentNullException.ThrowIfNull(showHidden);
        ArgumentNullException.ThrowIfNull(schemes);
        ArgumentNullException.ThrowIfNull(close);
        ArgumentNullException.ThrowIfNull(root);
        _showHidden = showHidden;
        _schemes = schemes;
        _close = close;
        _root = root;
    }

    /// <summary>Applies one intent the settings context mapped.</summary>
    /// <param name="intent">Mapped settings intent.</param>
    /// <returns>Whether the intent was a movement or activation this view applied.</returns>
    internal bool Forward(UserIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        Control[] items = Items();
        int current = IndexOfFocused(items);
        if (intent == UserIntent.MoveNext || intent == UserIntent.MovePrevious)
        {
            int next = SettingsFocusOrder.Move(current, items.Length, intent);
            if (next >= 0)
            {
                _ = items[next].Focus(FocusState.Keyboard);
            }
            return true;
        }
        if (intent == UserIntent.Confirm)
        {
            Activate(current < 0 ? null : items[current]);
            return true;
        }
        return false;
    }

    private static void Activate(Control? item)
    {
        if (item is RadioButton scheme)
        {
            scheme.IsChecked = true;
        }
        else if (item is CheckBox check)
        {
            check.IsChecked = check.IsChecked is not true;
        }
        else if (item is ButtonBase button &&
            Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(button) is
                Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
        {
            invoke.Invoke();
        }
    }

    private int IndexOfFocused(Control[] items)
    {
        object? focused = FocusManager.GetFocusedElement(_root());
        for (int index = 0; index < items.Length; index++)
        {
            if (ReferenceEquals(items[index], focused))
            {
                return index;
            }
        }
        return -1;
    }

    private Control[] Items()
    {
        return [_showHidden, .. Descendants(_schemes).OfType<RadioButton>(), _close];
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (DependencyObject descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
