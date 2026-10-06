using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using NeNeCommander.Application.Windowing;
using Windows.Graphics;

namespace NeNeCommander.App.Windowing;

/// <summary>
/// Translates the window's own <see cref="AppWindow"/>, presenter, display areas, and rasterization
/// scale into an Application <see cref="WindowPlacement"/>, and applies a decided plan to the same
/// window. It translates and never decides (ADR-0050): every geometry rule lives in
/// <see cref="WindowAdjustmentPlanner"/>, and every read failure becomes
/// <see cref="WindowPlacement.Unavailable"/> so the planner refuses instead of the host guessing.
/// </summary>
internal sealed class AppWindowPlacementAdapter
{
    private readonly AppWindow _window;
    private readonly UIElement _content;

    /// <summary>Initializes the adapter over one window and the content that carries its scale.</summary>
    /// <param name="window">The application window whose placement is read and written.</param>
    /// <param name="content">Window content whose <see cref="XamlRoot"/> reports the current scale.</param>
    internal AppWindowPlacementAdapter(AppWindow window, UIElement content)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(content);
        _window = window;
        _content = content;
    }

    /// <summary>Reads a fresh placement; a failed or incomplete read yields the unavailable placement.</summary>
    internal WindowPlacement Read()
    {
        try
        {
            return ReadPlacement();
        }
        catch (COMException)
        {
            return WindowPlacement.Unavailable;
        }
    }

    /// <summary>Applies one decided plan exactly as decided; a refused plan changes nothing.</summary>
    /// <param name="plan">Plan returned by the session for the placement read on the same keystroke.</param>
    internal void Apply(WindowAdjustmentPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        switch (plan)
        {
            case WindowMovePlan move:
                _window.MoveAndResize(RectOf(move.Bounds));
                break;
            case WindowResizePlan resize:
                _window.MoveAndResize(RectOf(resize.Bounds));
                break;
            case WindowRefusedPlan:
                break;
            default:
                ApplyPresenterPlan(plan);
                break;
        }
    }

    private void ApplyPresenterPlan(WindowAdjustmentPlan plan)
    {
        if (_window.Presenter is not OverlappedPresenter presenter)
        {
            throw new InvalidOperationException("A presenter plan requires the overlapped presenter it was decided for.");
        }
        if (plan == WindowAdjustmentPlan.Maximize)
        {
            presenter.Maximize();
            return;
        }
        if (plan == WindowAdjustmentPlan.Restore)
        {
            presenter.Restore();
            return;
        }
        throw new InvalidOperationException("The window adjustment plan is not supported.");
    }

    private WindowPlacement ReadPlacement()
    {
        XamlRoot? root = _content.XamlRoot;
        PointInt32 position = _window.Position;
        SizeInt32 size = _window.Size;
        RectInt32 current = DisplayArea.GetFromWindowId(_window.Id, DisplayAreaFallback.Nearest).WorkArea;
        List<RectInt32> attached = ReadAttachedWorkAreas();
        return root is null || !IsReadable(root.RasterizationScale, size, current, attached)
            ? WindowPlacement.Unavailable
            : WindowPlacement.Create(
                WindowBounds.Create(position.X, position.Y, size.Width, size.Height),
                ReadPresenterState(),
                ReadSizeConstraint(root.RasterizationScale),
                WindowWorkAreas.Create(BoundsOf(current), attached.ConvertAll(BoundsOf)));
    }

    private static bool IsReadable(
        double rasterizationScale,
        SizeInt32 size,
        RectInt32 current,
        List<RectInt32> attached)
    {
        return rasterizationScale > 0d &&
            size.Width >= 1 &&
            size.Height >= 1 &&
            attached.Contains(current) &&
            !attached.Exists(IsEmpty);
    }

    private WindowPresenterState ReadPresenterState()
    {
        return _window.Presenter is OverlappedPresenter presenter
            ? TranslateState(presenter.State)
            : WindowPresenterState.NotOverlapped;
    }

    private WindowSizeConstraint ReadSizeConstraint(double rasterizationScale)
    {
        // The SDK reports an undeclared preferred minimum as null; zero constrains nothing (ADR-0050).
        return _window.Presenter is OverlappedPresenter presenter
            ? WindowSizeConstraint.Create(
                rasterizationScale,
                presenter.PreferredMinimumWidth ?? 0,
                presenter.PreferredMinimumHeight ?? 0)
            : WindowSizeConstraint.Create(rasterizationScale, 0, 0);
    }

    private static WindowPresenterState TranslateState(OverlappedPresenterState state)
    {
        return state switch
        {
            OverlappedPresenterState.Restored => WindowPresenterState.Restored,
            OverlappedPresenterState.Maximized => WindowPresenterState.Maximized,
            OverlappedPresenterState.Minimized => WindowPresenterState.Minimized,
            _ => WindowPresenterState.Unavailable,
        };
    }

    private static List<RectInt32> ReadAttachedWorkAreas()
    {
        // Indexed access avoids enumerating the projected display collection, which older SDK
        // versions failed to enumerate through the generic iterator.
        IReadOnlyList<DisplayArea> displays = DisplayArea.FindAll();
        List<RectInt32> workAreas = [];
        for (int index = 0; index < displays.Count; index++)
        {
            workAreas.Add(displays[index].WorkArea);
        }
        return workAreas;
    }

    private static bool IsEmpty(RectInt32 rectangle)
    {
        return rectangle.Width < 1 || rectangle.Height < 1;
    }

    private static WindowBounds BoundsOf(RectInt32 rectangle)
    {
        return WindowBounds.Create(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    private static RectInt32 RectOf(WindowBounds bounds)
    {
        return new RectInt32(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
    }
}
