using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Renders the existing CommanderPaneView controls and forwards their typed intents.</summary>
internal sealed class CommanderPaneView
{
    private readonly FrameworkElement _root;
    private readonly ViewTextResources _resources;
    private readonly TextBlock _address;
    private readonly Button _navigation;
    private readonly TextBlock _status;
    private readonly TextBlock _counts;
    private readonly ListView _fileList;
    private readonly Border _border;
    private readonly Border _header;
    private readonly Border _numberSurface;
    private readonly TextBlock _number;
    private readonly PanePointerView _pointer;
    private PanePresentation? _rendered;
    private bool _rendering;


    internal CommanderPaneView(FrameworkElement root, ViewTextResources resources, PaneSide side, PanePointerBindings pointer)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(side);
        _root = root;
        _resources = resources;
        string prefix = side == PaneSide.Left ? "Left" : "Right";
        _address = Find<TextBlock>(prefix + "Address");
        _navigation = Find<Button>(prefix + "OpenOtherPane");
        _navigation.Click += (_, _) => pointer.Forward(UserIntent.OpenCurrentLocationInOtherPane);
        _status = Find<TextBlock>(prefix + "Status");
        _counts = Find<TextBlock>(prefix + "Counts");
        _fileList = Find<ListView>(prefix + "FileList");
        RowAutomationIdentity.Attach<PaneRow>(_fileList, row => row.AutomationId, row => row.Entry.Name);
        _border = Find<Border>(prefix + "PaneBorder");
        _header = Find<Border>(prefix + "PaneHeader");
        _numberSurface = Find<Border>(prefix + "PaneNumberSurface");
        _number = Find<TextBlock>(prefix + "PaneNumber");
        _pointer = new PanePointerView(_fileList, _header, side, pointer);
        _fileList.SelectionChanged += (_, _) => ReconcileSelection();
    }

    internal void Render(PanePresentation presentation, PaneFrame frame, PaneNavigationAction navigation)
    {
        _rendering = true;
        try
        {
            _pointer.Render(presentation);
            if (!ReferenceEquals(_rendered, presentation))
            {
                RenderContents(presentation, frame);
            }
            _rendered = presentation;
        }
        finally
        {
            _rendering = false;
        }
        ReconcileSelection();
        RenderNavigation(navigation);
        _status.Text = PaneStatusLine.Compose(
            presentation.Status, presentation.SortStatus, presentation.LocationStatus, _resources.GetString);
        _counts.Text = PaneCountsLine.Compose(presentation, _resources.GetString);
        ToolTipService.SetToolTip(_counts, _counts.Text);
    }

    private void RenderNavigation(PaneNavigationAction navigation)
    {
        _navigation.Visibility = navigation.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        _navigation.IsEnabled = navigation.IsEnabled;
        string navigationName = navigation.ComposeName(_resources.GetString);
        AutomationProperties.SetName(_navigation, navigationName);
        ToolTipService.SetToolTip(_navigation, navigationName);
    }

    private void RenderContents(PanePresentation presentation, PaneFrame frame)
    {
        _address.Text = presentation.AddressText;
        if (!ReferenceEquals(_fileList.ItemsSource, presentation.Rows))
        {
            _fileList.ItemsSource = presentation.Rows;
        }
        _fileList.SelectedItem = presentation.FocusRow;
        if (presentation.FocusRow is not null)
        {
            _fileList.ScrollIntoView(presentation.FocusRow);
        }
        RenderFrame(frame, _border, _header);
        RenderNumber(frame, _numberSurface, _number);
    }

    private void ReconcileSelection()
    {
        if (!_rendering && _rendered is not null &&
            !ReferenceEquals(_fileList.SelectedItem, _rendered.FocusRow))
        {
            _fileList.SelectedItem = _rendered.FocusRow;
        }
    }

    private static void RenderFrame(PaneFrame frame, Border border, Border header)
    {
        Brush brush = ResolveBrush(frame.BrushResourceKey);
        border.BorderBrush = brush;
        border.BorderThickness = (Thickness)Microsoft.UI.Xaml.Application.Current.Resources[frame.ThicknessResourceKey];
        header.BorderBrush = brush;
    }

    private static void RenderNumber(PaneFrame frame, Border surface, TextBlock number)
    {
        surface.Background = ResolveBrush(frame.NumberSurfaceBrushResourceKey);
        number.Foreground = ResolveBrush(frame.NumberForegroundBrushResourceKey);
    }

    private static Brush ResolveBrush(string resourceKey)
    {
        return (Brush)Microsoft.UI.Xaml.Application.Current.Resources[resourceKey];
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _root.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Pane control '{name}' is missing.");
    }
}
