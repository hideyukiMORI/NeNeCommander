using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Commands;
using NeNeCommander.Presentation.WinUI.Panes;
using Windows.Foundation;

namespace NeNeCommander.App.Views;

/// <summary>Adapts captured command selection to a native menu without choosing targets or dispatch policy.</summary>
internal sealed class CommandMenuView
{
    private readonly FrameworkElement _root;
    private readonly ViewTextResources _resources;
    private readonly Action<UserIntent> _forward;
    private readonly Action<PaneSide> _returnFocus;
    private CommandMenuInvocation? _invocation;
    private MenuFlyout? _menu;
    private CommandMenuPlacement? _placement;
    private bool _stopping;
    private object? _renderOwner;

    internal CommandMenuView(FrameworkElement root, ViewTextResources resources, Action<UserIntent> forward, Action<PaneSide> returnFocus)
    {
        _root = root;
        _resources = resources;
        _forward = forward;
        _returnFocus = returnFocus;
    }

    internal Action? Request(FrameworkElement anchor, Point point, UserIntent intent)
    {
        CommandMenuInvocation? before = _invocation;
        _placement = new CommandMenuPlacement(anchor, point);
        try
        {
            _forward(intent);
        }
        finally
        {
            _placement = null;
        }
        if (ReferenceEquals(before, _invocation) || _invocation is null || _menu is null)
        {
            return null;
        }
        CommandMenuInvocation invocation = _invocation;
        MenuFlyout menu = _menu;
        return () =>
        {
            if (!_stopping && ReferenceEquals(_invocation, invocation))
            {
                menu.Hide();
            }
        };
    }

    internal void Stop()
    {
        _stopping = true;
        Detach();
    }

    internal void Render(CommanderSnapshot snapshot)
    {
        if (_stopping || ReferenceEquals(_invocation?.SourceState, snapshot.Scopes.CommandPalette))
        {
            return;
        }
        object renderOwner = new();
        _renderOwner = renderOwner;
        bool wasOpen = _invocation is not null;
        Detach();
        if (_stopping || !ReferenceEquals(_renderOwner, renderOwner))
        {
            return;
        }
        if (snapshot.Scopes.CommandPalette is not CommandPaletteOpen open || open.Surface == CommandSurface.Palette)
        {
            if (wasOpen && snapshot.Scopes.CommandPalette is CommandPaletteClosed { FileListFocusSide: PaneSide side })
            {
                _returnFocus(side);
            }
            return;
        }
        CommandMenuInvocation invocation = new(open);
        MenuFlyout menu = CreateMenu(invocation);
        _invocation = invocation;
        _menu = menu;
        CommandMenuPlacement placement = _placement ?? KeyboardPlacement(open);
        menu.ShowAt(placement.Anchor, new FlyoutShowOptions
        {
            Position = placement.Point,
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
        });
    }

    private MenuFlyout CreateMenu(CommandMenuInvocation invocation)
    {
        MenuFlyout menu = new()
        {
            MenuFlyoutPresenterStyle = new Style(typeof(MenuFlyoutPresenter))
            {
                Setters = { new Setter(FrameworkElement.MaxWidthProperty, _root.ActualWidth) },
            },
        };
        foreach (CommandMenuSection section in CommandMenuPresenter.Present(invocation.SourceState, _resources.GetString))
        {
            MenuFlyoutSubItem group = new() { Text = section.Title };
            group.Resources["MenuFlyoutItemTextTrimming"] = TextTrimming.CharacterEllipsis;
            AutomationProperties.SetName(group, section.Title);
            ToolTipService.SetToolTip(group, section.Title);
            foreach (CommandPaletteRow row in section.Rows)
            {
                MenuFlyoutItem item = CreateItem(section.Title, row);
                item.Click += (_, _) => Forward(invocation.Submit(row.Candidate));
                group.Items.Add(item);
            }
            menu.Items.Add(group);
        }
        menu.Closed += (_, _) => invocation.Close(Enqueue, _forward);
        return menu;
    }

    private static MenuFlyoutItem CreateItem(string target, CommandPaletteRow row)
    {
        MenuFlyoutItem item = new()
        {
            Text = row.Title,
            IsEnabled = row.IsAvailable,
            KeyboardAcceleratorTextOverride = row.Shortcut,
        };
        string accessible = string.Join("; ", target, row.AutomationName);
        AutomationProperties.SetName(item, accessible);
        AutomationProperties.SetAutomationId(item, row.AutomationId);
        AutomationProperties.SetHelpText(item, row.Detail);
        ToolTipService.SetToolTip(item, accessible);
        return item;
    }

    private CommandMenuPlacement KeyboardPlacement(CommandPaletteOpen open)
    {
        string name = open.ActiveSide == PaneSide.Left ? "LeftFileList" : "RightFileList";
        ListView list = (ListView)_root.FindName(name);
        PaneSnapshot snapshot = open.ActiveSide == PaneSide.Left ? open.Left : open.Right;
        PaneState pane = ((PaneContentListed)snapshot.Content).State;
        PaneRow? focused = list.Items.OfType<PaneRow>().FirstOrDefault(row => ReferenceEquals(row.Entry.Path, pane.FocusItem));
        Point point = focused is not null && list.ContainerFromItem(focused) is ListViewItem container
            ? container.TransformToVisual(list).TransformPoint(new Point(0, container.ActualHeight))
            : new Point(0, 0);
        return new CommandMenuPlacement(list, point);
    }

    private void Enqueue(Action callback)
    {
        _ = _root.DispatcherQueue.TryEnqueue(() =>
        {
            if (!_stopping)
            {
                callback();
            }
        });
    }

    private void Forward(UserIntent? intent)
    {
        if (!_stopping && intent is not null)
        {
            _forward(intent);
        }
    }

    private void Detach()
    {
        _invocation?.Detach();
        _invocation = null;
        MenuFlyout? menu = _menu;
        _menu = null;
        menu?.Hide();
    }
}
