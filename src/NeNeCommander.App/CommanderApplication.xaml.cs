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
using NeNeCommander.Application.Sessions;
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
    private ResourceDictionary? _schemeDictionary;
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
        WindowsLocalIoExecutionBoundary ioExecutionBoundary = new();
        WindowsLocalSettingsStore settingsStore = new(
            WindowsLocalSettingsLocation.Resolve(),
            ioExecutionBoundary);
        SettingsReadOutcome settingsOutcome = await settingsStore.ReadAsync(cancellationToken).ConfigureAwait(true);
        UserSettings settings = settingsOutcome is SettingsRead read ? read.Settings : UserSettings.Default;
        ApplyColorScheme(settings.ColorScheme);
        SettingsSession settingsSession = new(settingsStore, settingsOutcome, ReportDefect);
        _window = CreateWindow(settings.HiddenItemVisibility, ioExecutionBoundary, settingsSession);
        _window.ColorSchemeChanged += OnColorSchemeChanged;
        ApplyElementTheme(_window, settings.ColorScheme.Appearance);
        _window.Closed += OnWindowClosed;
        _window.Activate();
    }

    /// <summary>
    /// Replaces the one composition-root scheme dictionary with the selected closed scheme.
    /// </summary>
    private void ApplyColorScheme(ColorScheme scheme)
    {
        if (_schemeDictionary is not null)
        {
            _ = Resources.MergedDictionaries.Remove(_schemeDictionary);
        }
        _schemeDictionary = new ResourceDictionary
        {
            Source = ColorSchemeResources.ResolveDictionaryAddress(scheme),
        };
        Resources.MergedDictionaries.Add(_schemeDictionary);
    }

    private static void ApplyElementTheme(Window window, ColorSchemeAppearance appearance)
    {
        ((FrameworkElement)window.Content).RequestedTheme = ColorSchemeResources.ResolveElementTheme(appearance);
    }

    /// <summary>
    /// Composes the window over the concrete boundary implementations. The persisted hidden-item
    /// visibility is passed to each pane session because it is part of pane state, not an observer:
    /// the pane needs it before its first read, and after that the state itself owns it.
    /// </summary>
    /// <param name="hiddenItemVisibility">Visibility both panes start from.</param>
    /// <param name="ioExecutionBoundary">Shared scheduler for synchronous Windows filesystem work.</param>
    /// <param name="settingsSession">Sole settings state and write owner.</param>
    private CommanderWindow CreateWindow(
        HiddenItemVisibility hiddenItemVisibility,
        WindowsLocalIoExecutionBoundary ioExecutionBoundary,
        SettingsSession settingsSession)
    {
        StopwatchClock clock = new();
        KeyboardIntentMapper keyboardIntentMapper = new(clock);
        ProviderDirectoryReadPort directoryReader = new(ioExecutionBoundary);
        VisiblePageCapacity capacity = CreateVisiblePageCapacity();
        _gateway = new FileOperationGateway(new ProviderFileOperationPort(ioExecutionBoundary));
        DualPaneSession panes = new(
            CreatePaneSession(directoryReader, capacity, hiddenItemVisibility),
            CreatePaneSession(directoryReader, capacity, hiddenItemVisibility),
            _gateway);
        CommanderSession session = new(panes, settingsSession);
        return new CommanderWindow(
            keyboardIntentMapper,
            session,
            ParseInitialLocation(InitialLeftLocationText),
            ParseInitialLocation(InitialRightLocationText),
            ReportDefect);
    }

    private void OnColorSchemeChanged(object? _, ColorSchemeChangedEventArgs args)
    {
        ApplyColorScheme(args.Scheme);
        if (_window is not null)
        {
            ApplyElementTheme(_window, args.Scheme.Appearance);
        }
    }

    private static PaneSession CreatePaneSession(
        IDirectoryReadPort directoryReader,
        VisiblePageCapacity capacity,
        HiddenItemVisibility hiddenItemVisibility)
    {
        return new PaneSession(
            directoryReader,
            capacity,
            DirectoryListing.EntryBoundaryLimit,
            hiddenItemVisibility);
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
                _window.ColorSchemeChanged -= OnColorSchemeChanged;
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
