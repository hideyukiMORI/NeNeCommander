using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the palette captures the declared command catalog and availability.</summary>
[TestClass]
public sealed class CommanderSessionPaletteCatalogTests
{
    /// <summary>Proves the palette exposes only the approved stable intent subset and state reasons.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteOpensCapturesStableCatalogAndAvailabilityAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);

        CommanderSnapshot opened = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            new RecordingCommanderObserver(),
            CancellationToken.None);

        CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>(opened.Scopes.CommandPalette);
        AssertPaletteIntentOrder(palette);
        Assert.AreSame(CommandAvailability.Available, CommanderSessionTestValues.Candidate(palette, UserIntent.OpenFocused).Availability);
        Assert.AreSame(
            CommandAvailability.Available,
            CommanderSessionTestValues.Candidate(palette, UserIntent.NavigateParent).Availability);
        Assert.AreEqual(
            CommandUnavailableReason.PassivePaneUnavailable,
            Assert.IsInstanceOfType<CommandUnavailable>(CommanderSessionTestValues.Candidate(palette, UserIntent.Copy).Availability).Reason);
        Assert.AreEqual(
            CommandUnavailableReason.BackUnavailable,
            Assert.IsInstanceOfType<CommandUnavailable>(CommanderSessionTestValues.Candidate(palette, UserIntent.NavigateBack).Availability).Reason);
        Assert.AreEqual(
            CommandUnavailableReason.ForwardUnavailable,
            Assert.IsInstanceOfType<CommandUnavailable>(CommanderSessionTestValues.Candidate(palette, UserIntent.NavigateForward).Availability).Reason);
        Assert.AreSame(opened.Panes.Left, palette.Left);
        Assert.AreSame(opened.Panes.Right, palette.Right);
        Assert.AreSame(PaneSide.Left, palette.ActiveSide);
    }

    /// <summary>Proves every no-source and root-navigation reason is captured without preflight.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteOpensOnEmptyRootCapturesUnavailableReasonsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.EmptyListing("C:\\"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\"), CancellationToken.None);

        CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            new RecordingCommanderObserver(),
            CancellationToken.None)).Scopes.CommandPalette);

        CommanderSessionTestValues.AssertUnavailable(palette, UserIntent.OpenFocused, CommandUnavailableReason.FocusRequired);
        CommanderSessionTestValues.AssertUnavailable(palette, UserIntent.Rename, CommandUnavailableReason.FocusRequired);
        CommanderSessionTestValues.AssertUnavailable(palette, UserIntent.ToggleSelection, CommandUnavailableReason.FocusRequired);
        CommanderSessionTestValues.AssertUnavailable(palette, UserIntent.Copy, CommandUnavailableReason.SourceRequired);
        CommanderSessionTestValues.AssertUnavailable(palette, UserIntent.Move, CommandUnavailableReason.SourceRequired);
        CommanderSessionTestValues.AssertUnavailable(palette, UserIntent.Delete, CommandUnavailableReason.SourceRequired);
        CommanderSessionTestValues.AssertUnavailable(palette, UserIntent.NavigateParent, CommandUnavailableReason.ParentUnavailable);
    }

    /// <summary>Proves an open palette freezes pane entry points and valid Escape preserves state.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteCancelsPreservesPanesAndReturnsToCapturedSideAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        PaneSnapshot before = session.Current.Panes.Left;
        _ = await session.HandleAsync(UserIntent.OpenCommandPalette, observer, CancellationToken.None);

        _ = await session.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\blocked"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>(session.Current.Scopes.CommandPalette);
        CommanderSnapshot cancelled = await session.HandleAsync(
            UserIntent.CancelCommandPalette(open),
            observer,
            CancellationToken.None);

        Assert.AreSame(before, cancelled.Panes.Left);
        Assert.AreSame(PaneSide.Left, cancelled.Panes.ActiveSide);
        Assert.HasCount(
            1,
            Assert.IsInstanceOfType<PaneContentListed>(cancelled.Panes.Left.Content).State.Selection);
        CommandPaletteClosed closed = Assert.IsInstanceOfType<CommandPaletteClosed>(cancelled.Scopes.CommandPalette);
        Assert.AreSame(PaneSide.Left, closed.FileListFocusSide);
        Assert.HasCount(1, left.Requests);
    }

    /// <summary>Proves unavailable and non-catalog submissions cannot dispatch or close the palette.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteSubmissionIsUnavailableOrUnknownKeepsPaletteAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);

        CommanderSnapshot unavailable = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.Copy),
            observer,
            CancellationToken.None);
        CommanderSnapshot unknown = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.Confirm),
            observer,
            CancellationToken.None);

        Assert.AreSame(open, unavailable.Scopes.CommandPalette);
        Assert.AreSame(open, unknown.Scopes.CommandPalette);
        Assert.AreSame(OperationActivity.Idle, unknown.Panes.Operation);
    }

    private static void AssertPaletteIntentOrder(CommandPaletteOpen palette)
    {
        CollectionAssert.AreEqual(
            new UserIntent[]
            {
                UserIntent.OpenFocused,
                UserIntent.NavigateParent,
                UserIntent.NavigateBack,
                UserIntent.NavigateForward,
                UserIntent.Refresh,
                UserIntent.FocusAddress,
                UserIntent.Rename,
                UserIntent.Copy,
                UserIntent.Move,
                UserIntent.CreateDirectory,
                UserIntent.Delete,
                UserIntent.ToggleSelection,
                UserIntent.ToggleHiddenItems,
                UserIntent.ActivateOtherPane,
                UserIntent.OpenSettings,
            UserIntent.OpenColorSchemes,
                UserIntent.SortByName,
                UserIntent.SortByExtension,
                UserIntent.OpenLocations,
                UserIntent.SortBySize,
                UserIntent.SortByModified,
                UserIntent.OpenBookmarks,
                UserIntent.OpenWindowAdjustment,
                UserIntent.OpenHelp,
            },
            palette.Candidates.Select(candidate => candidate.Intent).ToArray());
    }
}
