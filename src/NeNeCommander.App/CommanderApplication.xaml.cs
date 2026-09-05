using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using Microsoft.UI.Xaml;
using NeNeCommander.App.Themes;
using NeNeCommander.App.Views;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Directories;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;
using NeNeCommander.Infrastructure.Windows.Settings;
using NeNeCommander.Infrastructure.Windows.Time;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Lifecycle;

namespace NeNeCommander;

/// <summary>Owns the sole application composition root, the WinUI window lifetime, and the gateway it composes.</summary>
public sealed partial class CommanderApplication : Microsoft.UI.Xaml.Application, IDisposable
{
    /// <summary>
    /// Initial pane locations until drive discovery and persisted locations exist.
    /// They are parsed once here so no other layer interprets path text.
    /// </summary>
    private const string InitialLeftLocationText = "C:\\";

    private const string InitialRightLocationText = "C:\\Users";

    /// <summary>
    /// Visible rows assumed for half-page movement until the pane measures its own height.
    /// </summary>
    private const int AssumedVisibleRows = 20;

    private FileOperationGateway? _gateway;
    private readonly AsyncWorkOwner _shutdownWork;
    private readonly AsyncWorkOwner _startupWork;
    private CommanderWindow? _window;

    /// <summary>Initializes the WinUI application resources.</summary>
    public CommanderApplication()
    {
        _startupWork = new AsyncWorkOwner(ReportDefect);
        _shutdownWork = new AsyncWorkOwner(ReportDefect);
        InitializeComponent();
    }

    /// <summary>Composes the initial window from concrete boundary implementations.</summary>
    /// <param name="args">Framework launch details.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = _startupWork.TryStart(StartAsync);
    }

    /// <summary>Releases the composed gateway once the window has closed.</summary>
    public void Dispose()
    {
        _gateway?.Dispose();
        _gateway = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Reads persisted settings, applies the selected color scheme to the application resources
    /// before any view is created, and then composes and shows the window. The lifecycle owner
    /// observes the task because a framework launch handler cannot await.
    /// </summary>
    private async Task StartAsync(CancellationToken cancellationToken)
    {
        UserSettings settings = await ReadSettingsAsync(cancellationToken).ConfigureAwait(true);
        ApplyColorScheme(settings.ColorScheme);
        _window = CreateWindow();
        ApplyElementTheme(_window, settings.ColorScheme.Appearance);
        _window.Closed += OnWindowClosed;
        _window.Activate();
    }

    /// <summary>
    /// Reads settings through the sole settings boundary. An absent or rejected document keeps
    /// the default settings and leaves the stored document untouched (SEC-011).
    /// </summary>
    private static async Task<UserSettings> ReadSettingsAsync(CancellationToken cancellationToken)
    {
        WindowsLocalSettingsStore store = new(WindowsLocalSettingsLocation.Resolve());
        SettingsReadOutcome outcome = await store.ReadAsync(cancellationToken).ConfigureAwait(true);
        return outcome is SettingsRead read ? read.Settings : UserSettings.Default;
    }

    private void ApplyColorScheme(ColorScheme scheme)
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = ColorSchemeResources.ResolveDictionaryAddress(scheme),
        });
    }

    private static void ApplyElementTheme(Window window, ColorSchemeAppearance appearance)
    {
        ((FrameworkElement)window.Content).RequestedTheme = ColorSchemeResources.ResolveElementTheme(appearance);
    }

    private CommanderWindow CreateWindow()
    {
        StopwatchClock clock = new();
        KeyboardIntentMapper keyboardIntentMapper = new(clock);
        WindowsLocalIoExecutionBoundary ioExecutionBoundary = new();
        WindowsLocalDirectoryReader directoryReader = new(ioExecutionBoundary);
        VisiblePageCapacity capacity = CreateVisiblePageCapacity();
        _gateway = new FileOperationGateway(new WindowsLocalFileOperationAdapter(ioExecutionBoundary));
        DualPaneSession panes = new(
            new PaneSession(directoryReader, capacity, DirectoryListing.EntryBoundaryLimit),
            new PaneSession(directoryReader, capacity, DirectoryListing.EntryBoundaryLimit),
            _gateway);
        return new CommanderWindow(
            keyboardIntentMapper,
            panes,
            ParseInitialLocation(InitialLeftLocationText),
            ParseInitialLocation(InitialRightLocationText),
            ReportDefect);
    }

    private void OnWindowClosed(object _, WindowEventArgs args)
    {
        _ = _shutdownWork.TryStart(_ => ShutdownAsync());
    }

    private async Task ShutdownAsync()
    {
        try
        {
            if (_window is not null)
            {
                await _window.StopAsync().ConfigureAwait(true);
            }
        }
        finally
        {
            try
            {
                await _startupWork.StopAsync().ConfigureAwait(true);
            }
            finally
            {
                Dispose();
            }
        }
    }

    private static void ReportDefect(Exception defect)
    {
        ExceptionDispatchInfo.Capture(defect).Throw();
    }

    private static FileSystemPath ParseInitialLocation(string text)
    {
        return FileSystemPath.Parse(text) is PathParseSuccess location
            ? location.Path
            : throw new InvalidOperationException("The composed initial location is not a valid path.");
    }

    private static VisiblePageCapacity CreateVisiblePageCapacity()
    {
        return VisiblePageCapacity.Create(AssumedVisibleRows) is VisiblePageCapacityAccepted accepted
            ? accepted.Capacity
            : throw new InvalidOperationException("The composed visible-row capacity is not valid.");
    }
}
