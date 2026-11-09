using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Help;

namespace NeNeCommander.App.Views;

/// <summary>
/// Renders the keyboard help from the session state, owns its scrolling region and scrim, and
/// closes it through the session's synchronous route (ADR-0076). The groups come only from
/// <see cref="KeyboardHelpPresenter"/>; this view decides the column placement and scrolling.
/// </summary>
internal sealed class KeyboardHelpView
{
    private readonly Grid _overlay;
    private readonly CommanderSession _session;
    private readonly Action<PaneSide> _focusFileList;
    private readonly ScrollViewer _scroll;
    private readonly ItemsControl _secondary;
    private readonly double _lineStep;
    private readonly double _twoColumnMinWidth;
    private KeyboardHelpOpen? _rendered;

    internal KeyboardHelpView(
        Grid overlay,
        ResourceLoader resources,
        CommanderSession session,
        Action<PaneSide> focusFileList)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(focusFileList);
        _overlay = overlay;
        _session = session;
        _focusFileList = focusFileList;
        _scroll = Find<ScrollViewer>("KeyboardHelp");
        _secondary = Find<ItemsControl>("KeyboardHelpSecondaryGroups");
        _lineStep = Token("DensityRowHeight");
        _twoColumnMinWidth = Token("DensityKeyboardHelpTwoColumnMinWidth");
        KeyboardHelpPresentation help = KeyboardHelpPresenter.Present();
        Find<ItemsControl>("KeyboardHelpPrimaryGroups").ItemsSource = help.PrimaryGroups;
        _secondary.ItemsSource = help.SecondaryGroups;
        AutomationProperties.SetName(_scroll, resources.GetString("KeyboardHelpName"));
        Find<Border>("KeyboardHelpSurface").Tapped += OnSurfaceTapped;
        _overlay.Tapped += OnScrimTapped;
        _overlay.SizeChanged += OnOverlaySizeChanged;
    }

    /// <summary>Renders the state the session holds now.</summary>
    internal void Render()
    {
        KeyboardHelpState state = _session.Current.Scopes.KeyboardHelp;
        if (state is KeyboardHelpOpen open)
        {
            RenderOpen(open);
            return;
        }
        RenderClosed(state);
    }

    /// <summary>
    /// Forwards one intent the help context mapped. A close intent closes the help through the
    /// session with the open state last rendered; a movement intent only scrolls this view.
    /// </summary>
    /// <param name="intent">Intent the key map produced in the keyboard help context.</param>
    internal void Forward(UserIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (_rendered is not KeyboardHelpOpen expected)
        {
            return;
        }
        if (intent == UserIntent.Escape || intent == UserIntent.OpenHelp)
        {
            _ = _session.CloseKeyboardHelp(expected);
            Render();
            return;
        }
        double halfPage = _scroll.ViewportHeight / 2;
        double delta = intent == UserIntent.MoveNext ? _lineStep
            : intent == UserIntent.MovePrevious ? -_lineStep
            : intent == UserIntent.MoveHalfPageDown ? halfPage
            : intent == UserIntent.MoveHalfPageUp ? -halfPage
            : 0;
        _ = _scroll.ChangeView(null, _scroll.VerticalOffset + delta, null, true);
    }

    private void RenderOpen(KeyboardHelpOpen open)
    {
        if (ReferenceEquals(open, _rendered))
        {
            return;
        }
        bool opening = _rendered is null;
        _rendered = open;
        _overlay.Visibility = Visibility.Visible;
        if (opening)
        {
            ArrangeColumns(_overlay.ActualWidth);
            _ = _scroll.ChangeView(null, 0, null, true);
            _ = _scroll.Focus(FocusState.Programmatic);
        }
    }

    /// <summary>
    /// Returns focus to the file list captured at entry before collapsing, because collapsing a
    /// focused element lets the framework move focus to the first focusable control.
    /// </summary>
    private void RenderClosed(KeyboardHelpState state)
    {
        if (_rendered is null)
        {
            return;
        }
        _rendered = null;
        if (state is KeyboardHelpClosed { FileListFocusSide: PaneSide side })
        {
            _focusFileList(side);
        }
        _overlay.Visibility = Visibility.Collapsed;
    }

    private void OnOverlaySizeChanged(object sender, SizeChangedEventArgs args)
    {
        _ = sender;
        ArrangeColumns(args.NewSize.Width);
    }

    /// <summary>Places the secondary groups beside the primary column, or below it in a narrow window.</summary>
    private void ArrangeColumns(double width)
    {
        bool twoColumns = width >= _twoColumnMinWidth;
        Grid.SetColumn(_secondary, twoColumns ? 1 : 0);
        Grid.SetRow(_secondary, twoColumns ? 0 : 1);
    }

    private void OnScrimTapped(object sender, TappedRoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        Forward(UserIntent.Escape);
    }

    private void OnSurfaceTapped(object sender, TappedRoutedEventArgs args)
    {
        _ = sender;
        args.Handled = true;
    }

    private static double Token(string resourceKey)
    {
        return (double)Microsoft.UI.Xaml.Application.Current.Resources[resourceKey];
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _overlay.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Keyboard help control '{name}' is missing.");
    }
}
