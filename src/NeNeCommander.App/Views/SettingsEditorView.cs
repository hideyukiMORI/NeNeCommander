using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.App.Views;

/// <summary>Renders the settings editor and forwards native choices through its existing intent route.</summary>
internal sealed class SettingsEditorView
{
    private readonly Grid _root;
    private readonly ResourceLoader _resources;
    private readonly Action<UserIntent> _forward;
    private readonly Action _restoreFileListFocus;
    private readonly Grid _overlay;
    private readonly CheckBox _showHidden;
    private readonly CheckBox _showKeyHints;
    private readonly ItemsControl _languages;
    private readonly TextBlock _saveStatus;
    private readonly Border _warning;
    private readonly TextBlock _warningText;
    private readonly Button _close;
    private readonly SettingsKeyboardView _keyboard;
    private readonly Button _schemeButton;
    private readonly TextBlock _schemeName;
    private InterfaceLanguage? _selectedLanguage;
    private bool _initialFocusPending;
    private bool _rendering;
    private bool _returningFromPicker;

    internal SettingsEditorView(Grid root, ResourceLoader resources, Action<UserIntent> forward, Action restoreFileListFocus)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        ArgumentNullException.ThrowIfNull(restoreFileListFocus);
        _root = root;
        _resources = resources;
        _forward = forward;
        _restoreFileListFocus = restoreFileListFocus;
        _overlay = Find<Grid>("SettingsOverlay");
        _showHidden = Find<CheckBox>("SettingsShowHiddenAtLaunch");
        _showKeyHints = Find<CheckBox>("SettingsShowKeyHints");
        _languages = Find<ItemsControl>("SettingsLanguageOptions");
        _saveStatus = Find<TextBlock>("SettingsSaveStatus");
        _warning = Find<Border>("SettingsWarning");
        _warningText = Find<TextBlock>("SettingsWarningText");
        _close = Find<Button>("SettingsClose");
        _schemeButton = Find<Button>("SettingsChooseScheme");
        _schemeName = Find<TextBlock>("SettingsSelectedSchemeText");
        _schemeButton.Click += OnChooseScheme;
        _keyboard = new SettingsKeyboardView(
            [_showHidden, _showKeyHints, _languages, _schemeButton, _close],
            () => root.XamlRoot);
        _close.Loaded += OnCloseLoaded;
        _overlay.PreviewKeyDown += OnOverlayKeyDown;
        _overlay.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnOverlayPointerPressed), true);
    }

    internal bool IsOpen => _overlay.Visibility == Visibility.Visible;

    internal bool Forward(UserIntent intent)
    {
        _initialFocusPending = false;
        return _keyboard.Forward(intent);
    }

    internal void Render(SettingsPresentation presentation)
    {
        _rendering = true;
        _selectedLanguage = presentation.Languages.Single(static option => option.IsSelected).Language;
        bool opening = presentation.IsOpen && !IsOpen;
        if (!presentation.IsOpen && IsOpen)
        {
            _initialFocusPending = false;
            if (!presentation.IsSchemePickerOpen)
            {
                _restoreFileListFocus();
            }
        }
        _overlay.Visibility = presentation.IsOpen ? Visibility.Visible : Visibility.Collapsed;
        _showHidden.IsChecked = presentation.ShowHiddenItemsAtLaunch;
        _showKeyHints.IsChecked = presentation.ShowKeyHints;
        SettingsColorSchemeOption selected = presentation.Schemes.Single(static option => option.IsSelected);
        _schemeName.Text = _resources.GetString(selected.NameResourceKey);
        if (opening || _languages.ItemsSource is null)
        {
            _languages.ItemsSource = presentation.Languages;
        }
        _saveStatus.Text = _resources.GetString(presentation.SaveStatus.ResourceKey);
        _warning.Visibility = presentation.Warning.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        _warningText.Text = _resources.GetString(presentation.Warning.ResourceKey);
        if (opening)
        {
            FocusOpenedEditor();
        }
        _returningFromPicker = presentation.IsSchemePickerOpen;
        _rendering = false;
    }

    private void FocusOpenedEditor()
    {
        if (_returningFromPicker)
        {
            _ = _schemeButton.Focus(FocusState.Programmatic);
        }
        else
        {
            _initialFocusPending = true;
            FocusInitialClose();
        }
    }

    private void OnCloseLoaded(object sender, RoutedEventArgs args)
    {
        FocusInitialClose();
    }

    private void FocusInitialClose()
    {
        if (_initialFocusPending && IsOpen && _close.IsLoaded)
        {
            _initialFocusPending = false;
            _ = _close.Focus(FocusState.Programmatic);
        }
    }

    private void OnOverlayKeyDown(object sender, KeyRoutedEventArgs args)
    {
        _initialFocusPending = false;
    }

    private void OnOverlayPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        _initialFocusPending = false;
    }

    internal void ShowHiddenChanged(object sender)
    {
        if (!_rendering && sender is CheckBox { IsChecked: not null } checkbox)
        {
            HiddenItemVisibility visibility = checkbox.IsChecked is true
                ? HiddenItemVisibility.Shown
                : HiddenItemVisibility.Hidden;
            _forward(UserIntent.SelectLaunchHiddenItemVisibility(visibility));
        }
    }

    internal void ShowKeyHintsChanged(object sender)
    {
        if (!_rendering && sender is CheckBox { IsChecked: not null } checkbox)
        {
            KeyHintVisibility visibility = checkbox.IsChecked is true ? KeyHintVisibility.Shown : KeyHintVisibility.Hidden;
            _forward(UserIntent.SelectKeyHintVisibility(visibility));
        }
    }

    internal void LanguageChecked(object sender)
    {
        if (!_rendering && sender is RadioButton { IsChecked: true, DataContext: SettingsLanguageOption option } &&
            option.Language != _selectedLanguage)
        {
            _forward(UserIntent.SelectInterfaceLanguage(option.Language));
        }
    }

    private void OnChooseScheme(object sender, RoutedEventArgs args)
    {
        _forward(UserIntent.OpenColorSchemes);
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _root.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Settings editor control '{name}' is missing.");
    }
}
