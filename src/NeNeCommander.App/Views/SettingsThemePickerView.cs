using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.App.Views;

/// <summary>Adapts transient theme search to native controls and the existing explicit settings intent.</summary>
internal sealed class SettingsThemePickerView
{
    private readonly Grid _root;
    private readonly ResourceLoader _resources;
    private readonly Action<UserIntent> _forward;
    private readonly TextBox _search;
    private readonly ItemsControl _choices;
    private readonly TextBlock _current;
    private readonly TextBlock _results;
    private readonly Dictionary<SettingsSchemeRow, RadioButton> _realized = [];
    private SettingsSchemeViewState? _state;
    private bool _rendering;

    internal SettingsThemePickerView(Grid root, ResourceLoader resources, Action<UserIntent> forward)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        _root = root;
        _resources = resources;
        _forward = forward;
        _search = Find<TextBox>("SettingsSchemeSearch");
        _choices = Find<ItemsControl>("SettingsSchemeOptions");
        _current = Find<TextBlock>("SettingsCurrentScheme");
        _results = Find<TextBlock>("SettingsSchemeResults");
        _search.TextChanged += OnSearchChanged;
        _search.TextCompositionStarted += OnCompositionStarted;
        _search.TextCompositionEnded += OnCompositionEnded;
        _choices.GettingFocus += OnChoicesGettingFocus;
        _choices.GotFocus += OnChoicesGotFocus;
    }

    internal bool IsComposing { get; private set; }

    internal KeyboardContext OwnedKeyboardContext => ReferenceEquals(FocusManager.GetFocusedElement(_root.XamlRoot), _search)
        ? KeyboardContext.TextEntry : KeyboardContext.Settings;

    internal void Render(SettingsPresentation presentation)
    {
        _rendering = true;
        try
        {
            if (!presentation.IsOpen)
            {
                _state = null;
                IsComposing = false;
                _search.Text = string.Empty;
                _choices.ItemsSource = null;
                _realized.Clear();
                return;
            }
            _state ??= SettingsSchemeViewState.Create(presentation, _resources.GetString);
            _state.ObserveApplied(presentation.Schemes.Single(static option => option.IsSelected).Scheme);
            RenderRowsAndText();
            foreach (KeyValuePair<SettingsSchemeRow, RadioButton> entry in _realized)
            {
                entry.Value.IsChecked = entry.Key.Scheme == _state.CurrentRow.Scheme;
            }
        }
        finally
        {
            _rendering = false;
        }
    }

    internal void RowLoaded(object sender)
    {
        if (_state is not null && sender is RadioButton { DataContext: SettingsSchemeRow row } radio && _state.Rows.Contains(row))
        {
            _realized[row] = radio;
            bool rendering = _rendering;
            _rendering = true;
            try { radio.IsChecked = row.Scheme == _state.CurrentRow.Scheme; }
            finally { _rendering = rendering; }
        }
    }

    internal void RowUnloaded(object sender)
    {
        if (sender is RadioButton { DataContext: SettingsSchemeRow row } radio &&
            _realized.TryGetValue(row, out RadioButton? current) && ReferenceEquals(current, radio))
        {
            _ = _realized.Remove(row);
        }
    }

    internal void SchemeChecked(object sender)
    {
        if (!_rendering && _state is not null && sender is RadioButton { IsChecked: true, DataContext: SettingsSchemeRow row } radio &&
            _realized.TryGetValue(row, out RadioButton? current) && ReferenceEquals(current, radio) &&
            row.Scheme != _state.CurrentRow.Scheme)
        {
            _forward(UserIntent.SelectColorScheme(row.Scheme));
        }
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (!_rendering && _state is not null)
        {
            _state.UpdateQuery(_search.Text);
            RenderRowsAndText();
        }
    }

    private void RenderRowsAndText()
    {
        if (_state is not SettingsSchemeViewState state)
        {
            return;
        }
        if (!ReferenceEquals(_choices.ItemsSource, state.Rows))
        {
            _realized.Clear();
            _choices.ItemsSource = state.Rows;
        }
        _choices.Visibility = state.Rows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        _current.Text = string.Format(CultureInfo.InvariantCulture,
            CompositeFormat.Parse(_resources.GetString("SettingsCurrentSchemeFormat")), state.CurrentRow.Description);
        string results = state.Rows.Count == 0 ? _resources.GetString("SettingsSchemeNoResults") :
            string.Format(CultureInfo.InvariantCulture,
                CompositeFormat.Parse(_resources.GetString("SettingsSchemeResultsFormat")), state.Rows.Count, state.TotalCount);
        RenderResults(results);
    }

    private void RenderResults(string text)
    {
        if (string.Equals(_results.Text, text, StringComparison.Ordinal))
        {
            return;
        }
        _results.Text = text;
        FrameworkElementAutomationPeer.FromElement(_results)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private void OnChoicesGettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        _ = sender;
        if (args.Direction is FocusNavigationDirection.Next or FocusNavigationDirection.Previous &&
            !IsInsideChoices(args.OldFocusedElement) && _state?.TabEntry is SettingsSchemeRow entry &&
            _realized.TryGetValue(entry, out RadioButton? target) && !ReferenceEquals(args.NewFocusedElement, target))
        {
            _ = args.TrySetNewFocusedElement(target);
        }
    }

    private bool IsInsideChoices(DependencyObject? element)
    {
        while (element is not null)
        {
            if (ReferenceEquals(element, _choices))
            {
                return true;
            }
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    private void OnChoicesGotFocus(object sender, RoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (FocusManager.GetFocusedElement(_root.XamlRoot) is FrameworkElement focused)
        {
            focused.StartBringIntoView();
        }
    }

    private void OnCompositionStarted(TextBox sender, TextCompositionStartedEventArgs args)
    {
        _ = sender;
        _ = args;
        IsComposing = true;
    }

    private void OnCompositionEnded(TextBox sender, TextCompositionEndedEventArgs args)
    {
        _ = sender;
        _ = args;
        IsComposing = false;
    }

    private T Find<T>(string name) where T : DependencyObject
    {
        return _root.FindName(name) is T control ? control :
            throw new InvalidOperationException($"Settings theme picker control '{name}' is missing.");
    }
}
