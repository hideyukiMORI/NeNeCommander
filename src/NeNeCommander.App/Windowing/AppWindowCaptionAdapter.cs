using System;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NeNeCommander.Infrastructure.Windows.Windowing;
using Windows.Foundation;
using Windows.Graphics;

namespace NeNeCommander.App.Windowing;

/// <summary>
/// Removes the system title bar and its caption buttons from the window, declares the drawn
/// caption bar to the system as the window caption, and carries the three caption commands to the
/// window's own presenter and system close command (Issue #379). It translates and never decides:
/// the presenter state is the only record of maximized or restored, and closing enters the system
/// close path, so <c>AppWindow.Closing</c> and ADR-0071's drain run exactly as for Alt+F4.
/// </summary>
internal sealed class AppWindowCaptionAdapter
{
    private readonly AppWindow _window;
    private readonly FrameworkElement _dragArea;
    private readonly FrameworkElement _buttons;
    private readonly double _resizeBorder;
    private readonly double _buttonResizeBorder;

    /// <summary>Initializes the adapter over one window and the two parts of its caption bar.</summary>
    /// <param name="window">The application window whose frame and presenter are used.</param>
    /// <param name="dragArea">Caption-bar part that moves the window and opens the system menu.</param>
    /// <param name="buttons">Caption-bar part that holds the three caption buttons.</param>
    internal AppWindowCaptionAdapter(AppWindow window, FrameworkElement dragArea, FrameworkElement buttons)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(dragArea);
        ArgumentNullException.ThrowIfNull(buttons);
        _window = window;
        _dragArea = dragArea;
        _buttons = buttons;
        _resizeBorder = Token("DensityCaptionResizeBorder");
        _buttonResizeBorder = Token("DensityCaptionButtonResizeBorder");
    }

    /// <summary>Occurs after the presenter state may have changed, so the caption glyph is read again.</summary>
    internal event EventHandler? PresenterChanged;

    /// <summary>Gets whether the presenter reports the maximized state now.</summary>
    internal bool IsMaximized => Presenter.State == OverlappedPresenterState.Maximized;

    private OverlappedPresenter Presenter => _window.Presenter as OverlappedPresenter ??
        throw new InvalidOperationException("The caption bar requires the overlapped presenter.");

    /// <summary>
    /// Keeps the resize border, shadow, Snap and animations, and removes the system title bar with
    /// its caption buttons. Extending content into the title bar lets the drawn caption bar start at
    /// the top of the client area and lets the region declarations below take effect.
    /// </summary>
    internal void Attach()
    {
        _window.TitleBar.ExtendsContentIntoTitleBar = true;
        Presenter.SetBorderAndTitleBar(true, false);
        _window.Changed += OnWindowChanged;
        _dragArea.SizeChanged += OnPartSizeChanged;
        _buttons.SizeChanged += OnPartSizeChanged;
        _dragArea.Loaded += OnDragAreaLoaded;
    }

    /// <summary>Minimizes through the presenter.</summary>
    internal void Minimize()
    {
        Presenter.Minimize();
    }

    /// <summary>Maximizes or restores according to the state the presenter reports now.</summary>
    internal void ToggleMaximize()
    {
        OverlappedPresenter presenter = Presenter;
        if (presenter.State == OverlappedPresenterState.Maximized)
        {
            presenter.Restore();
            return;
        }
        presenter.Maximize();
    }

    /// <summary>Enters the system close path; the cancellable <c>AppWindow.Closing</c> follows.</summary>
    internal void RequestClose()
    {
        if (!WindowsSystemCloseRequest.Post(Win32Interop.GetWindowFromWindowId(_window.Id)))
        {
            throw new InvalidOperationException("The system refused the window close request.");
        }
    }

    private void OnDragAreaLoaded(object sender, RoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (_dragArea.XamlRoot is XamlRoot root)
        {
            root.Changed += OnXamlRootChanged;
        }
        DeclareRegions();
    }

    private void OnWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        _ = sender;
        if (args.DidPresenterChange || args.DidSizeChange)
        {
            DeclareRegions();
            PresenterChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnPartSizeChanged(object sender, SizeChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        DeclareRegions();
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        DeclareRegions();
    }

    /// <summary>
    /// Declares the drag area as the caption and, while restored, a top resize strip over the
    /// caption bar that is thinner over the buttons, so the buttons stay pressable while the top
    /// edge above them still resizes (NeNeNib's lesson). A maximized window has no resize strip.
    /// The buttons themselves stay ordinary client content. Rectangles are client-relative
    /// physical pixels.
    /// </summary>
    private void DeclareRegions()
    {
        if (_dragArea.XamlRoot is not XamlRoot root || _dragArea.ActualWidth <= 0 || _buttons.ActualWidth <= 0)
        {
            return;
        }
        double scale = root.RasterizationScale;
        InputNonClientPointerSource source = InputNonClientPointerSource.GetForWindowId(_window.Id);
        source.SetRegionRects(NonClientRegionKind.Caption, [PhysicalRect(_dragArea, scale, _dragArea.ActualHeight)]);
        if (IsMaximized)
        {
            source.ClearRegionRects(NonClientRegionKind.TopBorder);
            return;
        }
        source.SetRegionRects(NonClientRegionKind.TopBorder,
        [
            PhysicalRect(_dragArea, scale, _resizeBorder),
            PhysicalRect(_buttons, scale, _buttonResizeBorder),
        ]);
    }

    private static RectInt32 PhysicalRect(FrameworkElement element, double scale, double height)
    {
        GeneralTransform transform = element.TransformToVisual(null);
        Point origin = transform.TransformPoint(new Point(0, 0));
        return new RectInt32(
            (int)Math.Round(origin.X * scale),
            (int)Math.Round(origin.Y * scale),
            (int)Math.Round(element.ActualWidth * scale),
            (int)Math.Round(height * scale));
    }

    private static double Token(string resourceKey)
    {
        return (double)Microsoft.UI.Xaml.Application.Current.Resources[resourceKey];
    }
}
