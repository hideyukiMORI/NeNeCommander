using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.App.Windowing;

namespace NeNeCommander.App.Views;

/// <summary>
/// Renders the drawn caption bar (Issue #379, direction A): the application name, the three
/// caption buttons and their pointer and activation states, and the maximize or restore glyph read
/// from the presenter. It owns no window state; every command goes to
/// <see cref="AppWindowCaptionAdapter"/>. Every brush is a shared scheme brush, so a scheme switch
/// repaints the bar without a render.
/// </summary>
internal sealed class CaptionBarView
{
    private const string RestSurface = "SurfaceWindowBrush";
    private const string HoveredSurface = "SurfacePaneBrush";
    private const string PressedSurface = "BorderSubtleBrush";
    private readonly Grid _bar;
    private readonly AppWindowCaptionAdapter _adapter;
    private readonly string _maximizeName;
    private readonly string _restoreName;
    private readonly TextBlock _title;
    private readonly Button _maximize;
    private readonly Path _maximizeGlyph;
    private readonly Path _restoreGlyph;
    private readonly CaptionButtonPart[] _parts;
    private bool _active = true;

    internal CaptionBarView(Grid bar, ResourceLoader resources, AppWindowCaptionAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(bar);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(adapter);
        _bar = bar;
        _adapter = adapter;
        _maximizeName = resources.GetString("CaptionMaximizeName");
        _restoreName = resources.GetString("CaptionRestoreName");
        _title = Find<TextBlock>("CaptionTitle");
        _title.Text = resources.GetString("CommanderWindowTitle");
        _maximize = Find<Button>("CaptionMaximize");
        _maximizeGlyph = Find<Path>("CaptionMaximizeGlyph");
        _restoreGlyph = Find<Path>("CaptionRestoreGlyph");
        _parts =
        [
            new CaptionButtonPart(Find<Button>("CaptionMinimize"), [Find<Path>("CaptionMinimizeGlyph")]),
            new CaptionButtonPart(_maximize, [_maximizeGlyph, _restoreGlyph]),
            new CaptionButtonPart(Find<Button>("CaptionClose"), [Find<Path>("CaptionCloseGlyph")]),
        ];
        _parts[0].Button.Click += OnMinimizeClick;
        _parts[1].Button.Click += OnMaximizeClick;
        _parts[2].Button.Click += OnCloseClick;
        foreach (CaptionButtonPart part in _parts)
        {
            Attach(part);
        }
        _adapter.PresenterChanged += OnPresenterChanged;
        _adapter.Attach();
        RenderPresenter();
    }

    /// <summary>Renders the active or inactive appearance the window's activation reports.</summary>
    /// <param name="state">Activation state from <c>Window.Activated</c>.</param>
    internal void RenderActivation(WindowActivationState state)
    {
        _active = state != WindowActivationState.Deactivated;
        _title.Foreground = Brush(_active ? "TextSecondaryBrush" : "TextHiddenBrush");
        foreach (CaptionButtonPart part in _parts)
        {
            RenderPart(part);
        }
    }

    private void Attach(CaptionButtonPart part)
    {
        part.Button.PointerEntered += (_, _) => Update(part, HoveredSurface);
        part.Button.PointerExited += (_, _) => Update(part, RestSurface);
        part.Button.PointerCaptureLost += (_, _) => Update(part, RestSurface);
        part.Button.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, _) => Update(part, PressedSurface)), true);
        part.Button.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((_, _) => Update(part, HoveredSurface)), true);
    }

    private void Update(CaptionButtonPart part, string surfaceKey)
    {
        part.SurfaceKey = surfaceKey;
        RenderPart(part);
    }

    /// <summary>
    /// Pressed uses the border surface, hover the pane surface; both show the primary text color.
    /// At rest the glyph uses the key-hint color, or the hidden color while the window is inactive.
    /// The close button follows the same rule as the others (hide's decision of 2026-11-15).
    /// </summary>
    private void RenderPart(CaptionButtonPart part)
    {
        part.Button.Background = Brush(part.SurfaceKey);
        bool atRest = string.Equals(part.SurfaceKey, RestSurface, StringComparison.Ordinal);
        Brush stroke = Brush(!atRest ? "TextPrimaryBrush" : _active ? "TextKeyHintBrush" : "TextHiddenBrush");
        foreach (Path glyph in part.Glyphs)
        {
            glyph.Stroke = stroke;
        }
    }

    private void RenderPresenter()
    {
        bool maximized = _adapter.IsMaximized;
        _maximizeGlyph.Visibility = maximized ? Visibility.Collapsed : Visibility.Visible;
        _restoreGlyph.Visibility = maximized ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetName(_maximize, maximized ? _restoreName : _maximizeName);
    }

    private void OnPresenterChanged(object? sender, EventArgs args)
    {
        _ = sender;
        _ = args;
        RenderPresenter();
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        _adapter.Minimize();
    }

    private void OnMaximizeClick(object sender, RoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        _adapter.ToggleMaximize();
    }

    private void OnCloseClick(object sender, RoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        _adapter.RequestClose();
    }

    private static Brush Brush(string resourceKey)
    {
        return (Brush)Microsoft.UI.Xaml.Application.Current.Resources[resourceKey];
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _bar.FindName(name) is T element
            ? element
            : throw new InvalidOperationException($"Caption bar element '{name}' is missing.");
    }

    /// <summary>One caption button, its glyphs, and the surface its pointer state selects.</summary>
    private sealed class CaptionButtonPart
    {
        internal CaptionButtonPart(Button button, Path[] glyphs)
        {
            Button = button;
            Glyphs = glyphs;
        }

        internal Button Button { get; }

        internal Path[] Glyphs { get; }

        internal string SurfaceKey { get; set; } = RestSurface;
    }
}
