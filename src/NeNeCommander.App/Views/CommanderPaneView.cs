using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Renders the existing CommanderPaneView controls and forwards their typed intents.</summary>
internal sealed class CommanderPaneView
{
    private readonly FrameworkElement _root;
    private readonly ResourceLoader _resources;
    private readonly PaneSide _side;
    private readonly TextBox _address;
    private readonly TextBlock _status;
    private readonly ListView _fileList;
    private readonly Border _border;
    private readonly Border _header;
    private readonly Border _numberSurface;
    private readonly TextBlock _number;


    internal CommanderPaneView(FrameworkElement root, ResourceLoader resources, PaneSide side)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(side);
        _root = root;
        _resources = resources;
        _side = side;
        string prefix = side == PaneSide.Left ? "Left" : "Right";
        _address = Find<TextBox>(prefix + "Address");
        _status = Find<TextBlock>(prefix + "Status");
        _fileList = Find<ListView>(prefix + "FileList");
        _border = Find<Border>(prefix + "PaneBorder");
        _header = Find<Border>(prefix + "PaneHeader");
        _numberSurface = Find<Border>(prefix + "PaneNumberSurface");
        _number = Find<TextBlock>(prefix + "PaneNumber");
    }

    internal void Render(PanePresentation presentation, PaneFrame frame, AddressEditorPresentation? addressPresentation)
    {
        bool isEditingAddress = addressPresentation is { EditingSide: PaneSide editingSide } &&
            editingSide == _side;
        if (!isEditingAddress)
        {
            _address.Text = presentation.AddressText;
        }
        if (!ReferenceEquals(_fileList.ItemsSource, presentation.Rows))
        {
            _fileList.ItemsSource = presentation.Rows;
        }
        _fileList.SelectedItem = presentation.FocusRow;
        if (presentation.FocusRow is not null)
        {
            _fileList.ScrollIntoView(presentation.FocusRow);
        }
        PaneStatus paneStatus = isEditingAddress &&
            addressPresentation is { Status: PaneStatus addressStatus }
                ? addressStatus
                : presentation.Status;
        _status.Text = PaneStatusLine.Compose(
            paneStatus, presentation.SortStatus, presentation.LocationStatus, _resources.GetString);
        RenderFrame(frame, _border, _header);
        RenderNumber(frame, _numberSurface, _number);
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
