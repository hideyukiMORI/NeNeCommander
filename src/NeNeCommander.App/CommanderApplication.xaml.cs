using Microsoft.UI.Xaml;
using NeNeCommander.App.Views;
using NeNeCommander.Infrastructure.Windows.Time;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander;

/// <summary>Owns the sole application composition root and WinUI window lifetime.</summary>
public partial class CommanderApplication : Microsoft.UI.Xaml.Application
{
    private Window? _window;

    /// <summary>Initializes the WinUI application resources.</summary>
    public CommanderApplication()
    {
        InitializeComponent();
    }

    /// <summary>Composes the initial window from concrete boundary implementations.</summary>
    /// <param name="args">Framework launch details.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        StopwatchClock clock = new();
        KeyboardIntentMapper keyboardIntentMapper = new(clock);
        _window = new CommanderWindow(keyboardIntentMapper);
        _window.Activate();
    }
}
