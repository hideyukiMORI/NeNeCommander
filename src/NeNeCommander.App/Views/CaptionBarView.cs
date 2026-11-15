using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.App.Windowing;
using NeNeCommander.Presentation.WinUI.Windowing;

namespace NeNeCommander.App.Views;

/// <summary>
/// Renders the drawn caption bar (ADR-0078): the application name, the three caption buttons in
/// their pointer and activation states, and the maximize or restore glyph and name read from the
/// presenter each time it may have changed. Every appearance comes from
/// <see cref="CaptionBarPresenter"/>; this view only applies it and forwards each button to
/// <see cref="AppWindowCaptionAdapter"/>. Every brush is a shared scheme brush, so a scheme switch
/// repaints the bar without a render. Its subscriptions share the window's lifetime.
/// </summary>
internal sealed class CaptionBarView
{
    private readonly Grid _bar;
    private readonly ResourceLoader _resources;
    private readonly AppWindowCaptionAdapter _adapter;
    private readonly TextBlock _title;
    private readonly Button _maximize;
    private readonly Path _maximizeGlyph;
    private readonly Path _restoreGlyph;
    private readonly CaptionButtonPart[] _parts;
    private WindowActivity _activity = WindowActivity.Active;

    internal CaptionBarView(Grid bar, ResourceLoader resources, AppWindowCaptionAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(bar);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(adapter);
        _bar = bar;
        _resources = resources;
        _adapter = adapter;
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
        Render();
    }

    /// <summary>Renders the active or inactive appearance the window's activation reports.</summary>
    /// <param name="state">Activation state from <c>Window.Activated</c>.</param>
    internal void RenderActivation(WindowActivationState state)
    {
        _activity = state == WindowActivationState.Deactivated ? WindowActivity.Inactive : WindowActivity.Active;
        Render();
    }

    private void Attach(CaptionButtonPart part)
    {
        // The button's own pressed and pointer-over properties are the record of its pointer state.
        // They already account for pointer capture: a press dragged off the button and released
        // outside ends as neither pressed nor pointer-over, which separate pointer events misread.
        _ = part.Button.RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, (_, _) => Update(part));
        _ = part.Button.RegisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, (_, _) => Update(part));
    }

    private void Update(CaptionButtonPart part)
    {
        part.Pointer = part.Button.IsPressed
            ? CaptionButtonPointer.Pressed
            : part.Button.IsPointerOver ? CaptionButtonPointer.Hovered : CaptionButtonPointer.Rest;
        RenderPart(part);
    }

    private void Render()
    {
        CaptionBarPresentation presentation = CaptionBarPresenter.Present(_adapter.State, _activity);
        _title.Foreground = Brush(presentation.TitleBrushResourceKey);
        bool restore = presentation.MiddleButton == CaptionMiddleButton.Restore;
        _maximizeGlyph.Visibility = restore ? Visibility.Collapsed : Visibility.Visible;
        _restoreGlyph.Visibility = restore ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetName(_maximize, _resources.GetString(presentation.MiddleButton.NameResourceKey));
        foreach (CaptionButtonPart part in _parts)
        {
            RenderPart(part);
        }
    }

    private void RenderPart(CaptionButtonPart part)
    {
        CaptionButtonAppearance appearance = CaptionBarPresenter.PresentButton(part.Pointer, _activity);
        part.Button.Background = Brush(appearance.SurfaceBrushResourceKey);
        Brush stroke = Brush(appearance.GlyphBrushResourceKey);
        foreach (Path glyph in part.Glyphs)
        {
            glyph.Stroke = stroke;
        }
    }

    private void OnPresenterChanged(object? sender, EventArgs args)
    {
        _ = sender;
        _ = args;
        Render();
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

    /// <summary>One caption button, its glyphs, and the pointer state it shows.</summary>
    private sealed class CaptionButtonPart
    {
        internal CaptionButtonPart(Button button, Path[] glyphs)
        {
            Button = button;
            Glyphs = glyphs;
        }

        internal Button Button { get; }

        internal Path[] Glyphs { get; }

        internal CaptionButtonPointer Pointer { get; set; } = CaptionButtonPointer.Rest;
    }
}
