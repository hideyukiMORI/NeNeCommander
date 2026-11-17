using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the Commander session admits window adjustment only when its input scope is available.</summary>
[TestClass]
public sealed class CommanderSessionWindowAdjustmentTests
{
    /// <summary>Proves an idle session opens the mode on the active side with no outcome.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenIdleOpensTheModeOnTheActiveSideAsync()
    {
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);

        CommanderSnapshot opened = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);

        WindowAdjustmentOpen open = Assert.IsInstanceOfType<WindowAdjustmentOpen>(opened.Scopes.WindowAdjustment);
        Assert.AreSame(PaneSide.Right, open.ActiveSide);
        Assert.AreSame(WindowAdjustmentOutcome.None, open.Outcome);
        Assert.AreSame(open, fixture.Session.Current.Scopes.WindowAdjustment);
    }

    /// <summary>Proves listed content is not required: panes never read or whose read failed admit the mode.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPanesHoldNoListingStillOpensTheModeAsync()
    {
        using CommanderWindowAdjustmentTestContext unread = CommanderWindowAdjustmentTestContext.Create();
        using CommanderWindowAdjustmentTestContext failed = CommanderWindowAdjustmentTestContext.Create();
        failed.Left.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
        CommanderSnapshot afterFailure = await failed.Session.NavigateAsync(
            PaneSide.Left,
            CommanderWindowAdjustmentTestContext.ParsePath("C:\\missing"),
            CancellationToken.None);

        CommanderSnapshot unreadOpened = await unread.HandleAsync(UserIntent.OpenWindowAdjustment);
        CommanderSnapshot failedOpened = await failed.HandleAsync(UserIntent.OpenWindowAdjustment);

        Assert.AreSame(PaneContent.Absent, unreadOpened.Panes.Left.Content);
        _ = Assert.IsInstanceOfType<WindowAdjustmentOpen>(unreadOpened.Scopes.WindowAdjustment);
        _ = Assert.IsInstanceOfType<PaneReadFailed>(afterFailure.Panes.Left.Activity);
        _ = Assert.IsInstanceOfType<WindowAdjustmentOpen>(failedOpened.Scopes.WindowAdjustment);
    }

    /// <summary>Proves the open settings editor and bookmark manager keep input and refuse the mode.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSettingsOrBookmarksAreOpenRefusesTheModeAsync()
    {
        using CommanderWindowAdjustmentTestContext settings = await CommanderWindowAdjustmentTestContext.ListedAsync();
        using CommanderWindowAdjustmentTestContext bookmarks = await CommanderWindowAdjustmentTestContext.ListedAsync();
        _ = await settings.HandleAsync(UserIntent.OpenSettings);
        _ = await bookmarks.HandleAsync(UserIntent.OpenBookmarks);

        CommanderSnapshot settingsRefused = await settings.HandleAsync(UserIntent.OpenWindowAdjustment);
        CommanderSnapshot bookmarksRefused = await bookmarks.HandleAsync(UserIntent.OpenWindowAdjustment);

        Assert.AreSame(SettingsEditorState.Open, settingsRefused.Settings.Editor);
        Assert.AreSame(WindowAdjustmentState.Closed, settingsRefused.Scopes.WindowAdjustment);
        Assert.AreSame(SettingsEditorState.Bookmarks, bookmarksRefused.Settings.Editor);
        Assert.AreSame(WindowAdjustmentState.Closed, bookmarksRefused.Scopes.WindowAdjustment);
    }

    /// <summary>Proves direct input is refused by an address edit or palette, while a qualified palette submission opens the mode.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenAddressOrPaletteOwnsInputRefusesDirectModeAndAcceptsPaletteSubmissionAsync()
    {
        using CommanderWindowAdjustmentTestContext address = await CommanderWindowAdjustmentTestContext.ListedAsync();
        using CommanderWindowAdjustmentTestContext palette = await CommanderWindowAdjustmentTestContext.ListedAsync();
        AddressEditorState editor = (await address.HandleAsync(UserIntent.FocusAddress)).Scopes.AddressEditor;
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>(
            (await palette.HandleAsync(UserIntent.OpenCommandPalette)).Scopes.CommandPalette);

        CommanderSnapshot addressRefused = await address.HandleAsync(UserIntent.OpenWindowAdjustment);
        CommanderSnapshot paletteRefused = await palette.HandleAsync(UserIntent.OpenWindowAdjustment);
        CommanderSnapshot submissionAccepted = await palette.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.OpenWindowAdjustment));

        _ = Assert.IsInstanceOfType<AddressEditing>(editor);
        Assert.AreSame(editor, addressRefused.Scopes.AddressEditor);
        Assert.AreSame(WindowAdjustmentState.Closed, addressRefused.Scopes.WindowAdjustment);
        Assert.AreSame(open, paletteRefused.Scopes.CommandPalette);
        Assert.AreSame(CommandPaletteState.Closed, submissionAccepted.Scopes.CommandPalette);
        _ = Assert.IsInstanceOfType<WindowAdjustmentOpen>(submissionAccepted.Scopes.WindowAdjustment);
    }

    /// <summary>Proves a file operation awaiting a name, a confirmation, or a conflict decision refuses the mode.</summary>
    [TestMethod]
    [DataRow("name")]
    [DataRow("confirmation")]
    [DataRow("conflict")]
    public async Task HandleAsyncWhenOperationAwaitsADecisionRefusesTheModeAsync(string decision)
    {
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
        OperationActivity awaiting = await fixture.AwaitDecisionAsync(decision);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);

        Assert.AreSame(awaiting, refused.Panes.Operation);
        Assert.AreSame(WindowAdjustmentState.Closed, refused.Scopes.WindowAdjustment);
    }

    /// <summary>Proves a running file operation refuses the mode and still completes untouched.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOperationIsRunningRefusesTheModeAsync()
    {
        DirectoryListing leftListing = CommanderWindowAdjustmentTestContext.Listing("C:\\left", "item.txt");
        BlockingInspectionPort port = BlockingInspectionPort.Create(CommanderWindowAdjustmentTestContext.Inspection(leftListing.Entries[0].Path));
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync(port);
        Task<CommanderSnapshot> move = fixture.HandleAsync(UserIntent.Move);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);

        _ = Assert.IsInstanceOfType<OperationRunning>(refused.Panes.Operation);
        Assert.AreSame(WindowAdjustmentState.Closed, refused.Scopes.WindowAdjustment);
        _ = await fixture.CompleteBlockedOperationAsync(port, move);
    }

    /// <summary>Proves a pane read in flight on either side refuses the mode.</summary>
    [TestMethod]
    [DataRow("left")]
    [DataRow("right")]
    public async Task HandleAsyncWhenPaneReadIsInFlightRefusesTheModeAsync(string sideName)
    {
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
        PaneSide side = sideName == "left" ? PaneSide.Left : PaneSide.Right;
        TaskCompletionSource<DirectoryReadOutcome> read = fixture.PortOf(side).EnqueuePending();
        Task<CommanderSnapshot> pending = fixture.Session.NavigateAsync(
            side,
            CommanderWindowAdjustmentTestContext.ParsePath("C:\\target"),
            CancellationToken.None);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);

        _ = Assert.IsInstanceOfType<PaneLoading>(refused.Panes.Of(side).Activity);
        Assert.AreSame(WindowAdjustmentState.Closed, refused.Scopes.WindowAdjustment);
        read.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await pending;
    }

    /// <summary>Proves a file launch in flight on either side refuses the mode.</summary>
    [TestMethod]
    [DataRow("left")]
    [DataRow("right")]
    public async Task HandleAsyncWhenPaneLaunchIsInFlightRefusesTheModeAsync(string sideName)
    {
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
        PaneSide side = sideName == "left" ? PaneSide.Left : PaneSide.Right;
        if (side == PaneSide.Right)
        {
            _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);
        }
        TaskCompletionSource<FileLaunchOutcome> launch = fixture.LauncherOf(side).EnqueuePending();
        Task<CommanderSnapshot> pending = fixture.HandleAsync(UserIntent.OpenFocused);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);

        _ = Assert.IsInstanceOfType<PaneLaunching>(refused.Panes.Of(side).Activity);
        Assert.AreSame(WindowAdjustmentState.Closed, refused.Scopes.WindowAdjustment);
        launch.SetResult(FileLaunchOutcome.Accepted());
        _ = await pending;
    }
}
