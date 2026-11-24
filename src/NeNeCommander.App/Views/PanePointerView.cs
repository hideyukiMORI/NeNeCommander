using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NeNeCommander.App.Input;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Forwards passive pane hit targets and framework gestures without selecting command targets.</summary>
internal sealed class PanePointerView
{
    private readonly ListView _list;
    private readonly Border _header;
    private readonly PaneSide _side;
    private readonly PanePointerBindings _bindings;
    private PanePresentation? _rendered;

    internal PanePointerView(ListView list, Border header, PaneSide side, PanePointerBindings bindings)
    {
        _list = list;
        _header = header;
        _side = side;
        _bindings = bindings;
        _list.AddHandler(UIElement.TappedEvent, new TappedEventHandler(OnListTapped), true);
        _list.AddHandler(UIElement.DoubleTappedEvent, new DoubleTappedEventHandler(OnListDoubleTapped), true);
        _header.Tapped += OnHeaderTapped;
        _list.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);
        _list.PointerCanceled += (_, _) => _bindings.Mapper.Reset();
        _list.RightTapped += (_, _) => _bindings.Mapper.Reset();
    }

    internal void Render(PanePresentation presentation)
    {
        _rendered = presentation;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        if (!args.GetCurrentPoint(_list).Properties.IsLeftButtonPressed)
        {
            _bindings.Mapper.Reset();
        }
    }

    private void OnListTapped(object sender, TappedRoutedEventArgs args)
    {
        _bindings.Mapper.Reset();
        if (_rendered is null || args.PointerDeviceType != PointerDeviceType.Mouse)
        {
            return;
        }
        DependencyObject? target = FindPassiveTarget(args.OriginalSource as DependencyObject, _list);
        PanePointerModifier modifier = WinUiPanePointerTranslator.GetModifier();
        UserIntent? intent = target is ListViewItem { Content: PaneRow row }
            ? _bindings.Mapper.MapRowTap(_side, _rendered, row, modifier)
            : ReferenceEquals(target, _list)
                ? _bindings.Mapper.MapSurfaceTap(_side, _rendered, PanePointerSurface.ListBackground, modifier)
                : null;
        args.Handled = target is not null;
        Forward(intent);
    }

    private void OnListDoubleTapped(object sender, DoubleTappedRoutedEventArgs args)
    {
        if (_rendered is not null && args.PointerDeviceType == PointerDeviceType.Mouse &&
            FindPassiveTarget(args.OriginalSource as DependencyObject, _list) is ListViewItem { Content: PaneRow row })
        {
            args.Handled = true;
            Forward(_bindings.Mapper.MapRowDoubleTap(_side, _rendered, row, WinUiPanePointerTranslator.GetModifier()));
            return;
        }
        _bindings.Mapper.Reset();
    }

    private void OnHeaderTapped(object sender, TappedRoutedEventArgs args)
    {
        _bindings.Mapper.Reset();
        if (_rendered is not null && args.PointerDeviceType == PointerDeviceType.Mouse &&
            ReferenceEquals(FindPassiveTarget(args.OriginalSource as DependencyObject, _header), _header))
        {
            args.Handled = true;
            Forward(_bindings.Mapper.MapSurfaceTap(_side, _rendered, PanePointerSurface.Header,
                WinUiPanePointerTranslator.GetModifier()));
        }
    }

    private void Forward(UserIntent? intent)
    {
        if (intent is not null)
        {
            _bindings.Forward(intent);
        }
    }

    private static DependencyObject? FindPassiveTarget(DependencyObject? source, DependencyObject root)
    {
        while (source is not null)
        {
            if (ReferenceEquals(source, root) || source is ListViewItem)
            {
                return source;
            }
            if (source is Control and not ScrollViewer)
            {
                return null;
            }
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }
}
