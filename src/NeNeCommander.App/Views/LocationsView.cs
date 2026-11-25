using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Locations;

namespace NeNeCommander.App.Views;

/// <summary>
/// Renders the Locations picker from the session state, owns its scrim, list, and texts, and
/// forwards pointer selection and dismissal as qualified intents (ADR-0055). Keyboard input reaches
/// the session through the canonical mapper and <see cref="LocationsPresenter.Qualify"/>.
/// </summary>
internal sealed class LocationsView
{
    private readonly Grid _overlay;
    private readonly ViewTextResources _resources;
    private readonly Action<UserIntent> _forward;
    private readonly Action<PaneSide> _focusFileList;
    private readonly ListView _list;
    private readonly TextBlock _status;
    private readonly TextBlock _drives;
    private readonly TextBlock _wslRoots;
    private LocationsPresentation? _rendered;
    private PaneSide? _activeSide;

    internal LocationsView(
        Grid overlay,
        ViewTextResources resources,
        Action<UserIntent> forward,
        Action<PaneSide> focusFileList)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        ArgumentNullException.ThrowIfNull(focusFileList);
        _overlay = overlay;
        _resources = resources;
        _forward = forward;
        _focusFileList = focusFileList;
        _list = Find<ListView>("LocationsList");
        _status = Find<TextBlock>("LocationsStatus");
        _drives = Find<TextBlock>("LocationsDrivesStatus");
        _wslRoots = Find<TextBlock>("LocationsWslStatus");
        _list.ItemClick += OnItemClick;
        Find<Border>("LocationsModal").Tapped += OnSurfaceTapped;
        _overlay.Tapped += OnScrimTapped;
    }

    /// <summary>Gets the state last rendered, which qualifies the next mapped picker key.</summary>
    internal LocationsState Rendered => _rendered?.SourceState ?? LocationsState.Closed;

    /// <summary>Renders one picker state; an unchanged state is not rendered again.</summary>
    /// <param name="state">Picker state of the session snapshot being rendered.</param>
    internal void Render(LocationsState state)
    {
        if (ReferenceEquals(_rendered?.SourceState, state))
        {
            return;
        }
        LocationsPresentation presentation = LocationsPresenter.Present(state, _resources.GetString);
        bool opening = _rendered is not { IsShown: true } && presentation.IsShown;
        bool closing = _rendered is { IsShown: true } && !presentation.IsShown;
        _rendered = presentation;
        if (closing)
        {
            RenderClosed();
            return;
        }
        if (!presentation.IsShown)
        {
            return;
        }
        _activeSide = state is LocationsLoading loading ? loading.ActiveSide : ((LocationsOpen)state).ActiveSide;
        RenderShown(presentation);
        if (opening)
        {
            _ = _list.Focus(FocusState.Programmatic);
        }
    }

    private void RenderShown(LocationsPresentation presentation)
    {
        RenderText(_status, presentation.Texts.Status);
        RenderText(_drives, presentation.Texts.Drives);
        RenderText(_wslRoots, presentation.Texts.WslRoots);
        Find<ItemsControl>("LocationsKeyHints").ItemsSource = presentation.KeyHints;
        RenderRows(presentation);
        _overlay.Visibility = Visibility.Visible;
    }

    private void RenderRows(LocationsPresentation presentation)
    {
        // Retain measured containers during selection moves; resetting ItemsSource loses
        // variable row heights and can leave the selected wrapped row outside the viewport.
        if (!HasSameRows(presentation.Rows))
        {
            _list.ItemsSource = presentation.Rows;
        }
        LocationRow? selected = presentation.FocusRow is LocationRow target
            ? _list.Items.Cast<LocationRow>().First(row => ReferenceEquals(row.Item, target.Item))
            : null;
        _list.SelectedItem = selected;
        if (selected is LocationRow focus)
        {
            _list.ScrollIntoView(focus);
        }
    }

    private bool HasSameRows(IReadOnlyList<LocationRow> rows)
    {
        if (_list.ItemsSource is not IReadOnlyList<LocationRow> previous || previous.Count != rows.Count)
        {
            return false;
        }
        for (int index = 0; index < rows.Count; index++)
        {
            if (!ReferenceEquals(previous[index].Item, rows[index].Item) || previous[index] != rows[index])
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns focus to the file list of the pane active at open before collapsing, because
    /// collapsing a focused list lets the framework focus the left address box and begin an edit.
    /// </summary>
    private void RenderClosed()
    {
        if (_activeSide is PaneSide side)
        {
            _focusFileList(side);
        }
        _activeSide = null;
        _list.ItemsSource = null;
        _overlay.Visibility = Visibility.Collapsed;
    }

    private static void RenderText(TextBlock block, string text)
    {
        block.Text = text;
        block.Visibility = text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnItemClick(object sender, ItemClickEventArgs args)
    {
        _ = sender;
        if (_rendered?.SourceState is LocationsOpen open && args.ClickedItem is LocationRow row)
        {
            _forward(UserIntent.SelectLocation(open, row.Item));
        }
    }

    private void OnScrimTapped(object sender, TappedRoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (_rendered?.SourceState is LocationsOpen open)
        {
            _forward(UserIntent.CancelLocations(open));
        }
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
            : throw new InvalidOperationException($"Locations control '{name}' is missing.");
    }
}
