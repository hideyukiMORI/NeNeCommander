using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.App.Windowing;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Windowing;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Windowing;

namespace NeNeCommander.App.Views;

/// <summary>
/// Renders the window-adjustment helper from the session state, owns the helper's focus sink and
/// scrim, and forwards mapped mode keys through the synchronous window route (ADR-0050).
/// </summary>
internal sealed class WindowAdjustmentView
{
    private static readonly Dictionary<WindowAdjustmentKeyAction, WindowAdjustmentAction> Actions = new()
    {
        [WindowAdjustmentKeyAction.MoveLeft] = WindowAdjustmentAction.MoveLeft,
        [WindowAdjustmentKeyAction.MoveDown] = WindowAdjustmentAction.MoveDown,
        [WindowAdjustmentKeyAction.MoveUp] = WindowAdjustmentAction.MoveUp,
        [WindowAdjustmentKeyAction.MoveRight] = WindowAdjustmentAction.MoveRight,
        [WindowAdjustmentKeyAction.Enlarge] = WindowAdjustmentAction.Enlarge,
        [WindowAdjustmentKeyAction.Shrink] = WindowAdjustmentAction.Shrink,
        [WindowAdjustmentKeyAction.Maximize] = WindowAdjustmentAction.Maximize,
        [WindowAdjustmentKeyAction.Restore] = WindowAdjustmentAction.Restore,
    };

    private readonly Grid _overlay;
    private readonly ResourceLoader _resources;
    private readonly WindowAdjustmentRoute _route;
    private readonly Action<PaneSide> _focusFileList;
    private readonly ContentControl _sink;
    private readonly TextBlock _title;
    private readonly TextBlock _outcome;
    private WindowAdjustmentOpen? _rendered;

    internal WindowAdjustmentView(
        Grid overlay,
        ResourceLoader resources,
        WindowAdjustmentRoute route,
        Action<PaneSide> focusFileList)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(focusFileList);
        _overlay = overlay;
        _resources = resources;
        _route = route;
        _focusFileList = focusFileList;
        _sink = Find<ContentControl>("WindowAdjustmentHelper");
        _title = Find<TextBlock>("WindowAdjustmentTitle");
        _outcome = Find<TextBlock>("WindowAdjustmentOutcome");
        Find<ItemsControl>("WindowAdjustmentKeyHints").ItemsSource = WindowAdjustmentKeyHintPresenter.Present();
        Find<Border>("WindowAdjustmentSurface").Tapped += OnSurfaceTapped;
        _overlay.Tapped += OnScrimTapped;
    }

    /// <summary>
    /// Renders the state the session holds now rather than a captured snapshot, because the
    /// synchronous route can change the mode between a snapshot and its render.
    /// </summary>
    internal void Render()
    {
        if (_route.Current is WindowAdjustmentOpen open)
        {
            RenderOpen(open);
            return;
        }
        RenderClosed(_route.Current);
    }

    /// <summary>Forwards one mapped mode key with the open state last rendered, then renders.</summary>
    /// <param name="action">Mode action the key map produced.</param>
    internal void Forward(WindowAdjustmentKeyAction action)
    {
        if (_rendered is not WindowAdjustmentOpen expected)
        {
            return;
        }
        if (Actions.TryGetValue(action, out WindowAdjustmentAction? windowAction))
        {
            _route.Adjust(expected, windowAction);
        }
        else
        {
            _route.Leave(expected);
        }
        Render();
    }

    private void RenderOpen(WindowAdjustmentOpen open)
    {
        if (ReferenceEquals(open, _rendered))
        {
            return;
        }
        bool opening = _rendered is null;
        _rendered = open;
        WindowAdjustmentPresentation presentation = WindowAdjustmentPresenter.Present(open);
        string title = _resources.GetString(presentation.TitleResourceKey);
        string outcome = _resources.GetString(presentation.OutcomeResourceKey);
        _title.Text = title;
        _outcome.Text = outcome;
        _outcome.Foreground = (Brush)Microsoft.UI.Xaml.Application.Current.Resources[presentation.OutcomeBrushResourceKey];
        AutomationProperties.SetName(_sink, title);
        AutomationProperties.SetHelpText(_sink, outcome);
        _sink.Visibility = Visibility.Visible;
        _overlay.Visibility = Visibility.Visible;
        if (opening)
        {
            _ = _sink.Focus(FocusState.Programmatic);
            return;
        }
        FrameworkElementAutomationPeer.FromElement(_sink)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    /// <summary>
    /// Returns focus to the file list captured at entry before collapsing, because collapsing a
    /// focused sink lets the framework focus the left address box and begin an address edit.
    /// </summary>
    private void RenderClosed(WindowAdjustmentState state)
    {
        if (_rendered is null)
        {
            return;
        }
        _rendered = null;
        if (state is WindowAdjustmentClosed { FileListFocusSide: PaneSide side })
        {
            _focusFileList(side);
        }
        _sink.Visibility = Visibility.Collapsed;
        _overlay.Visibility = Visibility.Collapsed;
    }

    private void OnScrimTapped(object sender, TappedRoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        Forward(WindowAdjustmentKeyAction.Leave);
    }

    private void OnSurfaceTapped(object sender, TappedRoutedEventArgs args)
    {
        _ = sender;
        args.Handled = true;
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _overlay.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Window adjustment control '{name}' is missing.");
    }
}
