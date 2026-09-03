using System;
using Microsoft.UI.Xaml;
using NeNeCommander.App.Views;
using NeNeCommander.Application.Directories;
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
        _window = new CommanderWindow(keyboardIntentMapper, directoryReader, CreateInitialLeftRequest());
        _window.Activate();
    }

    private static DirectoryReadRequest CreateInitialLeftRequest()
    {
        if (FileSystemPath.Parse(InitialLeftLocationText) is not PathParseSuccess location)
        {
            throw new InvalidOperationException("The composed initial location is not a valid path.");
        }

        DirectoryReadRequestCreation creation = DirectoryReadRequest.Create(
            location.Path,
            DirectoryListing.EntryBoundaryLimit);
        return creation is DirectoryReadRequestAccepted accepted
            ? accepted.Request
            : throw new InvalidOperationException("The composed initial read request is not valid.");
    }
}
