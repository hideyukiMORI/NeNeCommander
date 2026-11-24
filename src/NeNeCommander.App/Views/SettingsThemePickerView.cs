using System;
using System.Globalization;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.App.Views;

/// <summary>Renders the one virtualized theme picker and applies visual previews through the host.</summary>
internal sealed class SettingsThemePickerView
{
    private readonly Grid _root;
    private readonly ViewTextResources _resources;
    private readonly Action<UserIntent> _forward;
    private readonly Action<ColorScheme> _preview;
    private readonly TextBox _search;
    private readonly ListView _choices;
    private readonly TextBlock _current;
    private readonly TextBlock _results;
    private readonly Button _cancel;
    private ColorSchemePickerOpen? _open;
    private SettingsSchemeViewState? _state;
    private bool _rendering;

    internal SettingsThemePickerView(Grid root, ViewTextResources resources, Action<UserIntent> forward, Action<ColorScheme> preview)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        ArgumentNullException.ThrowIfNull(preview);
        _root = root;
        _resources = resources;
        _forward = forward;
        _preview = preview;
        _search = Find<TextBox>("SettingsSchemeSearch");
        _choices = Find<ListView>("SettingsSchemeOptions");
        _current = Find<TextBlock>("SettingsCurrentScheme");
        _results = Find<TextBlock>("SettingsSchemeResults");
        _cancel = Find<Button>("ThemePickerCancel");
        RowAutomationIdentity.Attach<SettingsSchemeRow>(_choices, row => row.AutomationId, row => row.Description);
        _search.TextChanged += OnSearchChanged;
        _search.TextCompositionStarted += OnCompositionStarted;
        _search.TextCompositionEnded += OnCompositionEnded;
        _choices.SelectionChanged += OnSelectionChanged;
        _choices.ItemClick += OnItemClick;
        _cancel.Click += OnCancelClicked;
        _root.Tapped += OnScrimTapped;
        Find<Border>("ThemePickerSurface").Tapped += OnSurfaceTapped;
    }

    internal bool IsOpen => _open is not null;

    internal bool IsComposing { get; private set; }

    internal ColorScheme? PreviewScheme => _state?.PreviewScheme;

    internal void Render(SettingsSnapshot snapshot)
    {
        if (snapshot.Editor is not ColorSchemePickerOpen open)
        {
            Close();
            return;
        }
        bool opening = !ReferenceEquals(_open, open);
        if (opening)
        {
            Find<TextBlock>("ThemePickerInstructions").Text = ThemePickerKeyHintPresenter.Present(_resources.GetString);
            _open = open;
            _state = SettingsSchemeViewState.Create(SettingsPresenter.Present(snapshot), _resources.GetString);
            _rendering = true;
            _search.Text = string.Empty;
            _rendering = false;
        }
        _state?.ObserveApplied(snapshot.Settings.Display.ColorScheme);
        _root.Visibility = Visibility.Visible;
        RenderCandidates();
        if (opening)
        {
            _ = _search.Focus(FocusState.Programmatic);
        }
    }

    private void Close()
    {
        if (_open is null)
        {
            return;
        }
        _rendering = true;
        _open = null;
        _state = null;
        IsComposing = false;
        _choices.ItemsSource = null;
        _search.Text = string.Empty;
        _root.Visibility = Visibility.Collapsed;
        _rendering = false;
    }

    private void RenderCandidates()
    {
        if (_state is not SettingsSchemeViewState state)
        {
            return;
        }
        bool changed = !ReferenceEquals(_choices.ItemsSource, state.Rows) ||
            !ReferenceEquals(_choices.SelectedItem, state.SelectedRow);
        _rendering = true;
        if (!ReferenceEquals(_choices.ItemsSource, state.Rows))
        {
            _choices.ItemsSource = state.Rows;
        }
        _choices.SelectedItem = state.SelectedRow;
        _choices.IsTabStop = state.Rows.Count > 0;
        _rendering = false;
        _current.Text = string.Format(CultureInfo.InvariantCulture,
            CompositeFormat.Parse(_resources.GetString("SettingsCurrentSchemeFormat")), state.CurrentRow.Description);
        string results = state.Rows.Count == 0 ? _resources.GetString("SettingsSchemeNoResults") :
            string.Format(CultureInfo.InvariantCulture,
                CompositeFormat.Parse(_resources.GetString("SettingsSchemeResultsFormat")), state.Rows.Count, state.TotalCount);
        RenderResults(results);
        if (changed && state.SelectedRow is SettingsSchemeRow selected)
        {
            _choices.ScrollIntoView(selected);
        }
        _preview(state.PreviewScheme);
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

    private void OnSearchChanged(object sender, TextChangedEventArgs args)
    {
        if (!_rendering && _state is not null)
        {
            _state.UpdateQuery(_search.Text);
            RenderCandidates();
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!_rendering && _state is not null)
        {
            if (args.AddedItems.Count == 1 && args.AddedItems[0] is SettingsSchemeRow row)
            {
                _ = _state.Select(row);
            }
            RenderCandidates();
        }
    }

    private void OnItemClick(object sender, ItemClickEventArgs args)
    {
        if (_state is not null && args.ClickedItem is SettingsSchemeRow row && _state.Select(row))
        {
            Accept();
        }
    }

    internal bool Forward(CommandPaletteKeyAction action)
    {
        if (_state is not SettingsSchemeViewState state)
        {
            return true;
        }
        if (action == CommandPaletteKeyAction.MoveNext)
        {
            state.MoveNext();
            RenderCandidates();
        }
        else if (action == CommandPaletteKeyAction.MovePrevious)
        {
            state.MovePrevious();
            RenderCandidates();
        }
        else if (action == CommandPaletteKeyAction.Cancel)
        {
            Cancel();
        }
        else if (action == CommandPaletteKeyAction.Execute)
        {
            ExecuteFocusedControl();
        }
        else if (action == CommandPaletteKeyAction.MoveFocus)
        {
            return false;
        }
        return true;
    }

    private void ExecuteFocusedControl()
    {
        if (ReferenceEquals(FocusManager.GetFocusedElement(_root.XamlRoot), _cancel))
        {
            Cancel();
        }
        else
        {
            Accept();
        }
    }

    private void Accept()
    {
        if (!IsComposing && _open is ColorSchemePickerOpen open && _state?.SelectedRow is SettingsSchemeRow selected)
        {
            _forward(UserIntent.SubmitColorScheme(open, selected.Scheme));
        }
    }

    private void Cancel()
    {
        if (_open is ColorSchemePickerOpen open)
        {
            _forward(UserIntent.CancelColorSchemePicker(open));
        }
    }

    private void OnCancelClicked(object sender, RoutedEventArgs args)
    {
        Cancel();
    }

    private void OnScrimTapped(object sender, TappedRoutedEventArgs args)
    {
        Cancel();
    }

    private static void OnSurfaceTapped(object sender, TappedRoutedEventArgs args)
    {
        args.Handled = true;
    }

    private void OnCompositionStarted(TextBox sender, TextCompositionStartedEventArgs args)
    {
        IsComposing = true;
    }

    private void OnCompositionEnded(TextBox sender, TextCompositionEndedEventArgs args)
    {
        IsComposing = false;
    }

    private T Find<T>(string name) where T : DependencyObject
    {
        return _root.FindName(name) is T control ? control :
            throw new InvalidOperationException($"Theme picker control '{name}' is missing.");
    }
}
