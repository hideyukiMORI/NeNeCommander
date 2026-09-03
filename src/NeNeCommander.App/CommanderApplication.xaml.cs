using System;
using Microsoft.UI.Xaml;
using NeNeCommander.App.Views;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Directories;
using NeNeCommander.Infrastructure.Windows.Time;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander;

/// <summary>Owns the sole application composition root and WinUI window lifetime.</summary>
public partial class CommanderApplication : Microsoft.UI.Xaml.Application
{
    /// <summary>
    /// Initial left-pane location until drive discovery and persisted locations exist.
    /// It is parsed once here so no other layer interprets path text.
    /// </summary>
    private const string InitialLeftLocationText = "C:\\";

    /// <summary>
    /// Visible rows assumed for half-page movement until the pane measures its own height.
    /// </summary>
    private const int AssumedVisibleRows = 20;

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
        WindowsLocalDirectoryReader directoryReader = new();
        PaneSession leftPane = new(directoryReader, CreateVisiblePageCapacity(), DirectoryListing.EntryBoundaryLimit);
        _window = new CommanderWindow(keyboardIntentMapper, leftPane, ParseInitialLeftLocation());
        _window.Activate();
    }

    private static FileSystemPath ParseInitialLeftLocation()
    {
        return FileSystemPath.Parse(InitialLeftLocationText) is PathParseSuccess location
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
