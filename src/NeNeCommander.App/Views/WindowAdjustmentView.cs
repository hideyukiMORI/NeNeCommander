using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
    private readonly Grid _overlay;
    private readonly ViewTextResources _resources;
    private readonly WindowAdjustmentRoute _route;
    private readonly Action<PaneSide> _focusFileList;
    private readonly ContentControl _sink;
    private readonly TextBlock _title;
    private readonly TextBlock _outcome;
    private readonly ScrollViewer _scroll;
    private WindowAdjustmentOpen? _pendingScrollReset;
    private WindowAdjustmentOpen? _resetIssuedFor;
    private WindowAdjustmentOpen? _rendered;
    private bool _stopping;

    internal WindowAdjustmentView(
        Grid overlay,
        ViewTextResources resources,
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
        _scroll = Find<ScrollViewer>("WindowAdjustmentBody");
        BindHints();
        _scroll.LayoutUpdated += OnScrollLayoutUpdated;
        Find<Border>("WindowAdjustmentSurface").Tapped += OnSurfaceTapped;
        _overlay.Tapped += OnScrimTapped;
    }

    /// <summary>
    /// Renders the state the session holds now rather than a captured snapshot, because the
    /// synchronous route can change the mode between a snapshot and its render.
    /// </summary>
    internal void Render()
    {
        if (_stopping)
        {
            return;
        }
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
        if (_stopping || _rendered is not WindowAdjustmentOpen expected)
        {
            return;
        }
        if (action == WindowAdjustmentKeyAction.PreviousHelpPage || action == WindowAdjustmentKeyAction.NextHelpPage)
        {
            ScrollPage(expected, action);
            return;
        }
        if (action.WindowAction is WindowAdjustmentAction windowAction)
        {
            _route.Adjust(expected, windowAction);
        }
        else if (action == WindowAdjustmentKeyAction.Leave)
        {
            _route.Leave(expected);
        }
        Render();
    }

    /// <summary>Stops forwarding and rendering before close capture can reenter this view.</summary>
    internal void Stop()
    {
        _stopping = true;
        _rendered = null;
        _pendingScrollReset = null;
        _resetIssuedFor = null;
        _scroll.LayoutUpdated -= OnScrollLayoutUpdated;
        _route.Stop();
    }
    private void RenderOpen(WindowAdjustmentOpen open)
    {
        if (_stopping || ReferenceEquals(open, _rendered) || !ReferenceEquals(open, _route.Current))
        {
            return;
        }
        bool opening = _rendered is null;
        _rendered = open;
        _pendingScrollReset = null;
        _resetIssuedFor = null;
        RenderText(open, WindowAdjustmentPresenter.Present(open));
        if (!CanRenderOpen(open))
        {
            return;
        }
        _sink.Visibility = Visibility.Visible;
        if (!CanRenderOpen(open))
        {
            return;
        }
        _overlay.Visibility = Visibility.Visible;
        if (!CanRenderOpen(open))
        {
            return;
        }
        _pendingScrollReset = open;
        TryResetScroll();
        if (!CanRenderOpen(open))
        {
            return;
        }
        if (opening)
        {
            _ = _sink.Focus(FocusState.Programmatic);
            return;
        }
        FrameworkElementAutomationPeer.FromElement(_sink)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private void RenderText(WindowAdjustmentOpen open, WindowAdjustmentPresentation presentation)
    {
        string title = _resources.GetString(presentation.TitleResourceKey);
        string outcome = _resources.GetString(presentation.OutcomeResourceKey);
        _title.Text = title;
        if (!CanRenderOpen(open))
        {
            return;
        }
        _outcome.Text = outcome;
        if (!CanRenderOpen(open))
        {
            return;
        }
        _outcome.Foreground = (Brush)Microsoft.UI.Xaml.Application.Current.Resources[presentation.OutcomeBrushResourceKey];
        if (!CanRenderOpen(open))
        {
            return;
        }
        AutomationProperties.SetName(_sink, title);
        if (!CanRenderOpen(open))
        {
            return;
        }
        AutomationProperties.SetHelpText(_sink, outcome);
    }

    private bool CanRenderOpen(WindowAdjustmentOpen open)
    {
        return !_stopping && ReferenceEquals(open, _rendered) && ReferenceEquals(open, _route.Current);
    }

    private bool CanRenderClosed(WindowAdjustmentState state)
    {
        return !_stopping && _rendered is null && ReferenceEquals(state, _route.Current);
    }

    /// <summary>Returns captured pane focus before collapse, unless a new owner or stop invalidates this render.</summary>
    private void RenderClosed(WindowAdjustmentState state)
    {
        if (_stopping || _rendered is null || !ReferenceEquals(state, _route.Current))
        {
            return;
        }
        _rendered = null;
        _pendingScrollReset = null;
        _resetIssuedFor = null;
        if (state is WindowAdjustmentClosed { FileListFocusSide: PaneSide side })
        {
            _focusFileList(side);
        }
        if (!CanRenderClosed(state))
        {
            return;
        }
        _sink.Visibility = Visibility.Collapsed;
        if (!CanRenderClosed(state))
        {
            return;
        }
        _overlay.Visibility = Visibility.Collapsed;
    }
    private void BindHints()
    {
        IReadOnlyList<WindowAdjustmentKeyHint> hints = WindowAdjustmentKeyHintPresenter.Present();
        Find<ItemsControl>("WindowAdjustmentKeyHints").ItemsSource = hints.Where(hint =>
            hint.Group != WindowAdjustmentHintGroup.Leave && hint.Group != WindowAdjustmentHintGroup.ScrollHelp).ToArray();
        Find<ItemsControl>("WindowAdjustmentFooterHints").ItemsSource = hints.Where(hint =>
            hint.Group == WindowAdjustmentHintGroup.Leave || hint.Group == WindowAdjustmentHintGroup.ScrollHelp).ToArray();
    }

    private void OnScrollLayoutUpdated(object? sender, object args)
    {
        _ = sender;
        _ = args;
        TryResetScroll();
    }

    private void TryResetScroll()
    {
        if (_pendingScrollReset is not WindowAdjustmentOpen expected || !CanRenderOpen(expected))
        {
            return;
        }
        if (!_scroll.IsLoaded || _scroll.ViewportHeight <= 0d)
        {
            return;
        }
        _pendingScrollReset = null;
        _ = _scroll.ChangeView(null, 0d, null, true);
        if (CanRenderOpen(expected))
        {
            _resetIssuedFor = expected;
        }
    }

    private void ScrollPage(WindowAdjustmentOpen expected, WindowAdjustmentKeyAction action)
    {
        if (!CanRenderOpen(expected))
        {
            return;
        }
        _overlay.UpdateLayout();
        TryResetScroll();
        if (!CanRenderOpen(expected) || !ReferenceEquals(_resetIssuedFor, expected))
        {
            return;
        }
        double page = _scroll.ViewportHeight / 2d;
        double delta = action == WindowAdjustmentKeyAction.PreviousHelpPage ? -page : page;
        double offset = Math.Clamp(_scroll.VerticalOffset + delta, 0d, _scroll.ScrollableHeight);
        _ = _scroll.ChangeView(null, offset, null, true);
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
