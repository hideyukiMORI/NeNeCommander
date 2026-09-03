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
    /// Initial pane locations until drive discovery and persisted locations exist.
    /// They are parsed once here so no other layer interprets path text.
    /// </summary>
    private const string InitialLeftLocationText = "C:\\";

    private const string InitialRightLocationText = "C:\\Users";

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
        VisiblePageCapacity capacity = CreateVisiblePageCapacity();
        DualPaneSession panes = new(
            new PaneSession(directoryReader, capacity, DirectoryListing.EntryBoundaryLimit),
            new PaneSession(directoryReader, capacity, DirectoryListing.EntryBoundaryLimit));
        _window = new CommanderWindow(
            keyboardIntentMapper,
            panes,
            ParseInitialLocation(InitialLeftLocationText),
            ParseInitialLocation(InitialRightLocationText));
        _window.Activate();
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
