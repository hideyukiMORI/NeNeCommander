using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace NeNeCommander.App.Windowing;

/// <summary>Translates a system close request into the application's owned shutdown request.</summary>
internal sealed partial class AppWindowCloseAdapter
{
    private readonly AppWindow _appWindow;
    private readonly Action _requestClose;

    /// <summary>Attaches the cancellable close event for one XAML window.</summary>
    internal AppWindowCloseAdapter(Window window, Action requestClose)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(requestClose);
        _appWindow = window.AppWindow;
        _requestClose = requestClose;
        _appWindow.Closing += OnClosing;
    }

    /// <summary>Detaches before the composition root performs its final programmatic close.</summary>
    internal void Detach()
    {
        _appWindow.Closing -= OnClosing;
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        _ = sender;
        args.Cancel = true;
        _requestClose();
    }
}
