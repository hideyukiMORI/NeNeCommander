using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NeNeCommander.Application.Windowing;
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
    private readonly Action<Exception> _defectObserver;
    private readonly double _resizeBorder;
    private readonly double _resizeCorner;
    private XamlRoot? _observedRoot;

    /// <summary>Initializes the adapter over one window and the two parts of its caption bar.</summary>
    /// <param name="window">The application window whose frame and presenter are used.</param>
    /// <param name="dragArea">Caption-bar part that moves the window and opens the system menu.</param>
    /// <param name="buttons">Caption-bar part that holds the three caption buttons.</param>
    /// <param name="defectObserver">Host callback that publishes an unexpected platform defect.</param>
    internal AppWindowCaptionAdapter(
        AppWindow window,
        FrameworkElement dragArea,
        FrameworkElement buttons,
        Action<Exception> defectObserver)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(dragArea);
        ArgumentNullException.ThrowIfNull(buttons);
        ArgumentNullException.ThrowIfNull(defectObserver);
        _window = window;
        _dragArea = dragArea;
        _buttons = buttons;
        _defectObserver = defectObserver;
        _resizeBorder = Token("DensityCaptionResizeBorder");
        _resizeCorner = Token("DensityCaptionResizeCorner");
    }

    /// <summary>Occurs after the presenter state may have changed, so the caption glyph is read again.</summary>
    internal event EventHandler? PresenterChanged;

    /// <summary>Gets the presenter state the window reports now; it is read, never stored.</summary>
    internal WindowPresenterState State => _window.Presenter is OverlappedPresenter presenter
        ? AppWindowPlacementAdapter.TranslateState(presenter.State)
        : WindowPresenterState.NotOverlapped;

    private OverlappedPresenter Presenter => _window.Presenter as OverlappedPresenter ??
        throw new InvalidOperationException("The caption bar requires the overlapped presenter.");

    /// <summary>
    /// Keeps the resize border, shadow, Snap and animations, and removes the system title bar with
    /// its caption buttons (ADR-0078). Extending content into the title bar lets the drawn caption
    /// bar start at the top of the client area and lets the region declarations below take effect.
    /// The subscriptions are never detached: the adapter, the window and its caption bar elements
    /// share one lifetime, and the process ends when that window closes (ADR-0071).
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
        Run(MinimizePresenter);
    }

    private void MinimizePresenter()
    {
        Presenter.Minimize();
    }

    /// <summary>Maximizes or restores according to the state the presenter reports now.</summary>
    internal void ToggleMaximize()
    {
        Run(() =>
        {
            OverlappedPresenter presenter = Presenter;
            if (presenter.State == OverlappedPresenterState.Maximized)
            {
                presenter.Restore();
                return;
            }
            presenter.Maximize();
        });
    }

    /// <summary>
    /// Enters the system close path, the same <c>SC_CLOSE</c> as Alt+F4; the cancellable
    /// <c>AppWindow.Closing</c> and ADR-0071's drain follow. It never closes the window itself.
    /// </summary>
    internal void RequestClose()
    {
        Run(() =>
        {
            if (!WindowsSystemCloseRequest.Post(Win32Interop.GetWindowFromWindowId(_window.Id)))
            {
                throw new InvalidOperationException("The system refused the window close request.");
            }
        });
    }

    /// <summary>
    /// Runs one platform call; a failure is a defect, reported through the existing observer like
    /// every other host defect (the same rule as <see cref="WindowAdjustmentRoute"/>).
    /// </summary>
    private void Run(Action call)
    {
        try
        {
            call();
        }
        catch (COMException defect)
        {
            _defectObserver(defect);
        }
        catch (InvalidOperationException defect)
        {
            _defectObserver(defect);
        }
        catch (ArgumentException defect)
        {
            _defectObserver(defect);
        }
    }

    /// <summary>Observes the rasterization scale of the current root once, however often Loaded runs.</summary>
    private void OnDragAreaLoaded(object sender, RoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        XamlRoot? root = _dragArea.XamlRoot;
        if (!ReferenceEquals(root, _observedRoot))
        {
            _observedRoot?.Changed -= OnXamlRootChanged;
            _observedRoot = root;
            _observedRoot?.Changed += OnXamlRootChanged;
        }
        Run(DeclareRegions);
    }

    private void OnWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        _ = sender;
        if (args.DidPresenterChange || args.DidSizeChange)
        {
            Run(DeclareRegions);
            PresenterChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnPartSizeChanged(object sender, SizeChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        Run(DeclareRegions);
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        Run(DeclareRegions);
    }
    /// <summary>
    /// Declares the drag area as the caption. While restored it also declares resize edges at the
    /// top of the caption bar: a top strip across the whole bar, including over the buttons, and a
    /// strip down each side of the bar, all <c>DensityCaptionResizeBorder</c> deep. The side strips
    /// replace the system's default side declarations, which also cover only the caption height.
    /// The system answers a top-left or top-right corner where a top rectangle overlaps a side
    /// rectangle (Issue #379 measurement of 2026-11-15), so a top and a side rectangle of the same
    /// <c>DensityCaptionResizeCorner</c> square make each top corner that square. The top edge has
    /// no system resize frame outside the window, so these declarations are the only top resize
    /// targets (NeNeNib's lesson: buttons must not cover them). A maximized window declares no
    /// resize edge. The buttons stay ordinary client content below the strip. Rectangles are
    /// client-relative physical pixels. Product code never reads the declarations back:
    /// <c>GetRegionRects</c> throws <c>E_POINTER</c> for a kind without one (ADR-0078).
    /// </summary>
    private void DeclareRegions()
    {
        if (_dragArea.XamlRoot is not XamlRoot root || _dragArea.ActualWidth <= 0 || _buttons.ActualWidth <= 0)
        {
            return;
        }
        double scale = root.RasterizationScale;
        InputNonClientPointerSource source = InputNonClientPointerSource.GetForWindowId(_window.Id);
        RectInt32 drag = PhysicalRect(_dragArea, scale);
        RectInt32 buttons = PhysicalRect(_buttons, scale);
        source.SetRegionRects(NonClientRegionKind.Caption, [drag]);
        if (State == WindowPresenterState.Maximized)
        {
            source.ClearRegionRects(NonClientRegionKind.TopBorder);
            source.ClearRegionRects(NonClientRegionKind.LeftBorder);
            source.ClearRegionRects(NonClientRegionKind.RightBorder);
            return;
        }
        int edge = Physical(_resizeBorder, scale);
        int corner = Physical(_resizeCorner, scale);
        int right = buttons.X + buttons.Width;
        RectInt32 leftCorner = new(drag.X, drag.Y, corner, corner);
        RectInt32 rightCorner = new(right - corner, drag.Y, corner, corner);
        source.SetRegionRects(NonClientRegionKind.TopBorder,
            [new RectInt32(drag.X, drag.Y, right - drag.X, edge), leftCorner, rightCorner]);
        source.SetRegionRects(NonClientRegionKind.LeftBorder,
            [leftCorner, new RectInt32(drag.X, drag.Y, edge, drag.Height)]);
        source.SetRegionRects(NonClientRegionKind.RightBorder,
            [rightCorner, new RectInt32(right - edge, buttons.Y, edge, buttons.Height)]);
    }

    private static RectInt32 PhysicalRect(FrameworkElement element, double scale)
    {
        GeneralTransform transform = element.TransformToVisual(null);
        Point origin = transform.TransformPoint(new Point(0, 0));
        return new RectInt32(
            Physical(origin.X, scale),
            Physical(origin.Y, scale),
            Physical(element.ActualWidth, scale),
            Physical(element.ActualHeight, scale));
    }

    private static int Physical(double logical, double scale)
    {
        return (int)Math.Round(logical * scale);
    }

    private static double Token(string resourceKey)
    {
        return (double)Microsoft.UI.Xaml.Application.Current.Resources[resourceKey];
    }
}
