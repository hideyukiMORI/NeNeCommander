using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Commands;

namespace NeNeCommander.App.Views;

/// <summary>Renders the command-entry buttons and forwards their existing typed intents.</summary>
internal sealed class CommandEntryView
{
    private readonly StackPanel _root;
    private readonly Button[] _buttons;
    private readonly ViewTextResources _resources;
    private readonly Action<UserIntent> _forward;

    internal CommandEntryView(StackPanel root, ViewTextResources resources, Action<UserIntent> forward)
    {
        _root = root;
        _resources = resources;
        _forward = forward;
        _buttons = [.. root.Children.OfType<Button>()];
        foreach (Button button in _buttons)
        {
            button.Click += OnClick;
        }
    }

    internal void Render(CommandEntryPresentation presentation)
    {
        for (int index = 0; index < _buttons.Length; index++)
        {
            Button button = _buttons[index];
            CommandEntryButton entry = presentation.Buttons[index];
            button.Tag = entry.Intent;
            button.IsEnabled = presentation.IsEnabled;
            AutomationProperties.SetName(button, _resources.GetString(entry.LabelResourceKey));
            AutomationProperties.SetAcceleratorKey(button, _resources.GetString(entry.ShortcutResourceKey));
            ToolTipService.SetToolTip(button, CommandEntryPresenter.ToolTipFor(entry, _resources.GetString));
        }
    }

    internal bool ContainsFocus(object? focused)
    {
        DependencyObject? current = focused as DependencyObject;
        while (current is not null)
        {
            if (ReferenceEquals(current, _root))
            {
                return true;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    internal void FocusFirst()
    {
        _ = _buttons.FirstOrDefault(button => button.IsEnabled)?.Focus(FocusState.Keyboard);
    }

    private void OnClick(object sender, RoutedEventArgs args)
    {
        if (sender is Button { IsEnabled: true, Tag: UserIntent intent })
        {
            _forward(intent);
        }
    }
}
