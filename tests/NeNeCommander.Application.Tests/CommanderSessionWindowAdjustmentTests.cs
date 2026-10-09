using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Application.Windowing;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>
/// Proves how the application session admits, freezes, and delegates the window-adjustment mode,
/// and that its two synchronous members decide only inside the mode's own scope.
/// </summary>
[TestClass]
public sealed class CommanderSessionWindowAdjustmentTests
{
    /// <summary>Proves an idle session opens the mode on the active side with no outcome.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenIdleOpensTheModeOnTheActiveSideAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
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
        using Fixture unread = Fixture.Create();
        using Fixture failed = Fixture.Create();
        failed.Left.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
        CommanderSnapshot afterFailure = await failed.Session.NavigateAsync(
            PaneSide.Left,
            ParsePath("C:\\missing"),
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
        using Fixture settings = await Fixture.ListedAsync();
        using Fixture bookmarks = await Fixture.ListedAsync();
        _ = await settings.HandleAsync(UserIntent.OpenSettings);
        _ = await bookmarks.HandleAsync(UserIntent.OpenBookmarks);

        CommanderSnapshot settingsRefused = await settings.HandleAsync(UserIntent.OpenWindowAdjustment);
        CommanderSnapshot bookmarksRefused = await bookmarks.HandleAsync(UserIntent.OpenWindowAdjustment);

        Assert.AreSame(SettingsEditorState.Open, settingsRefused.Settings.Editor);
        Assert.AreSame(WindowAdjustmentState.Closed, settingsRefused.Scopes.WindowAdjustment);
        Assert.AreSame(SettingsEditorState.Bookmarks, bookmarksRefused.Settings.Editor);
        Assert.AreSame(WindowAdjustmentState.Closed, bookmarksRefused.Scopes.WindowAdjustment);
    }

    /// <summary>Proves an address edit and an open palette keep input and refuse the mode, even as a palette submission.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenAddressOrPaletteOwnsInputRefusesTheModeAsync()
    {
        using Fixture address = await Fixture.ListedAsync();
        using Fixture palette = await Fixture.ListedAsync();
        AddressEditorState editor = (await address.HandleAsync(UserIntent.FocusAddress)).Scopes.AddressEditor;
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>(
            (await palette.HandleAsync(UserIntent.OpenCommandPalette)).Scopes.CommandPalette);

        CommanderSnapshot addressRefused = await address.HandleAsync(UserIntent.OpenWindowAdjustment);
        CommanderSnapshot paletteRefused = await palette.HandleAsync(UserIntent.OpenWindowAdjustment);
        CommanderSnapshot submissionRefused = await palette.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.OpenWindowAdjustment));

        _ = Assert.IsInstanceOfType<AddressEditing>(editor);
        Assert.AreSame(editor, addressRefused.Scopes.AddressEditor);
        Assert.AreSame(WindowAdjustmentState.Closed, addressRefused.Scopes.WindowAdjustment);
        Assert.AreSame(open, paletteRefused.Scopes.CommandPalette);
        Assert.AreSame(open, submissionRefused.Scopes.CommandPalette);
        Assert.AreSame(WindowAdjustmentState.Closed, submissionRefused.Scopes.WindowAdjustment);
    }

    /// <summary>Proves a file operation awaiting a name, a confirmation, or a conflict decision refuses the mode.</summary>
    [TestMethod]
    [DataRow("name")]
    [DataRow("confirmation")]
    [DataRow("conflict")]
    public async Task HandleAsyncWhenOperationAwaitsADecisionRefusesTheModeAsync(string decision)
    {
        using Fixture fixture = await Fixture.ListedAsync();
        OperationActivity awaiting = await fixture.AwaitDecisionAsync(decision);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);

        Assert.AreSame(awaiting, refused.Panes.Operation);
        Assert.AreSame(WindowAdjustmentState.Closed, refused.Scopes.WindowAdjustment);
    }

    /// <summary>Proves a running file operation refuses the mode and still completes untouched.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOperationIsRunningRefusesTheModeAsync()
    {
        DirectoryListing leftListing = Listing("C:\\left", "item.txt");
        BlockingInspectionPort port = BlockingInspectionPort.Create(Inspection(leftListing.Entries[0].Path));
        using Fixture fixture = await Fixture.ListedAsync(port);
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
        using Fixture fixture = await Fixture.ListedAsync();
        PaneSide side = sideName == "left" ? PaneSide.Left : PaneSide.Right;
        TaskCompletionSource<DirectoryReadOutcome> read = fixture.PortOf(side).EnqueuePending();
        Task<CommanderSnapshot> pending = fixture.Session.NavigateAsync(
            side,
            ParsePath("C:\\target"),
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
        using Fixture fixture = await Fixture.ListedAsync();
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

    /// <summary>
    /// Proves the open mode freezes every pane, settings, bookmark, address, palette, and operation
    /// intent, and that external navigation is refused without a read.
    /// </summary>
    [TestMethod]
    public async Task HandleAsyncWhenModeIsOpenFreezesEveryIntentAndNavigationAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        CommanderSnapshot before = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);
        WindowAdjustmentOpen open = Assert.IsInstanceOfType<WindowAdjustmentOpen>(before.Scopes.WindowAdjustment);
        UserIntent[] intents =
        [
            UserIntent.MoveNext,
            UserIntent.ActivateOtherPane,
            UserIntent.ToggleHiddenItems,
            UserIntent.Refresh,
            UserIntent.OpenFocused,
            UserIntent.Escape,
            UserIntent.Confirm,
            UserIntent.Copy,
            UserIntent.Move,
            UserIntent.Delete,
            UserIntent.Rename,
            UserIntent.CreateDirectory,
            UserIntent.FocusAddress,
            UserIntent.OpenSettings,
            UserIntent.OpenBookmarks,
            UserIntent.OpenCommandPalette,
            UserIntent.OpenWindowAdjustment,
            UserIntent.BookmarkSlotOne,
        ];

        foreach (UserIntent intent in intents)
        {
            CommanderSnapshot frozen = await fixture.HandleAsync(intent);
            Fixture.AssertUnchanged(before, frozen);
            Assert.AreSame(open, frozen.Scopes.WindowAdjustment);
        }
        CommanderSnapshot navigation = await fixture.Session.NavigateAsync(
            PaneSide.Right,
            ParsePath("C:\\other"),
            CancellationToken.None);

        Fixture.AssertUnchanged(before, navigation);
        Assert.AreSame(open, navigation.Scopes.WindowAdjustment);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
        Assert.IsEmpty(fixture.Launcher.Targets);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves each action returns the planner's exact plan and records it as the open outcome.</summary>
    [TestMethod]
    public async Task AdjustWindowWhenModeIsOpenReturnsTheExactPlanForEachActionAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);
        WindowPlacement restored = WindowAdjustmentSessionTests.Placement(WindowPresenterState.Restored);
        WindowPlacement maximized = WindowAdjustmentSessionTests.Placement(WindowPresenterState.Maximized);

        Assert.AreEqual(WindowBounds.Create(68, 100, 800, 600), fixture.Move(WindowAdjustmentAction.MoveLeft, restored));
        Assert.AreEqual(WindowBounds.Create(100, 132, 800, 600), fixture.Move(WindowAdjustmentAction.MoveDown, restored));
        Assert.AreEqual(WindowBounds.Create(100, 68, 800, 600), fixture.Move(WindowAdjustmentAction.MoveUp, restored));
        Assert.AreEqual(WindowBounds.Create(132, 100, 800, 600), fixture.Move(WindowAdjustmentAction.MoveRight, restored));
        Assert.AreEqual(WindowBounds.Create(100, 100, 832, 632), fixture.Resize(WindowAdjustmentAction.Enlarge, restored));
        Assert.AreEqual(WindowBounds.Create(100, 100, 768, 568), fixture.Resize(WindowAdjustmentAction.Shrink, restored));
        Assert.AreSame(WindowAdjustmentPlan.Maximize, fixture.Plan(WindowAdjustmentAction.Maximize, restored));
        Assert.AreSame(WindowAdjustmentPlan.Restore, fixture.Plan(WindowAdjustmentAction.Restore, maximized));
        WindowAdjustmentOpen last = Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            fixture.Session.Current.Scopes.WindowAdjustment);
        Assert.AreSame(
            WindowAdjustmentAction.Restore,
            Assert.IsInstanceOfType<WindowActionPlanned>(last.Outcome).Action);
    }

    /// <summary>Proves maximize and restore are idempotent commands whose repetition is refused, not toggled.</summary>
    [TestMethod]
    public async Task AdjustWindowWhenMaximizeOrRestoreRepeatsRefusesInsteadOfTogglingAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);
        WindowPlacement restored = WindowAdjustmentSessionTests.Placement(WindowPresenterState.Restored);
        WindowPlacement maximized = WindowAdjustmentSessionTests.Placement(WindowPresenterState.Maximized);
        WindowPlacement minimized = WindowAdjustmentSessionTests.Placement(WindowPresenterState.Minimized);

        Assert.AreSame(WindowAdjustmentPlan.Maximize, fixture.Plan(WindowAdjustmentAction.Maximize, restored));
        Assert.AreSame(WindowAdjustmentPlan.Maximize, fixture.Plan(WindowAdjustmentAction.Maximize, minimized));
        fixture.AssertRefused(WindowAdjustmentAction.Maximize, maximized, WindowAdjustmentRefusal.WindowIsMaximized);
        Assert.AreSame(WindowAdjustmentPlan.Restore, fixture.Plan(WindowAdjustmentAction.Restore, maximized));
        Assert.AreSame(WindowAdjustmentPlan.Restore, fixture.Plan(WindowAdjustmentAction.Restore, minimized));
        fixture.AssertRefused(WindowAdjustmentAction.Restore, restored, WindowAdjustmentRefusal.WindowIsRestored);
    }

    /// <summary>Proves every planner action is refused in a maximized, minimized, non-overlapped, or unavailable placement.</summary>
    [TestMethod]
    public async Task AdjustWindowWhenPlacementBlocksActionRecordsTheRefusalAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);
        WindowPlacement maximized = WindowAdjustmentSessionTests.Placement(WindowPresenterState.Maximized);
        WindowPlacement minimized = WindowAdjustmentSessionTests.Placement(WindowPresenterState.Minimized);
        WindowPlacement notOverlapped = WindowAdjustmentSessionTests.Placement(WindowPresenterState.NotOverlapped);

        foreach (WindowAdjustmentAction action in WindowAdjustmentPlannerTests.AllActions())
        {
            fixture.AssertRefused(action, notOverlapped, WindowAdjustmentRefusal.PresenterIsNotOverlapped);
            fixture.AssertRefused(action, WindowPlacement.Unavailable, WindowAdjustmentRefusal.PlacementUnavailable);
            if (action != WindowAdjustmentAction.Maximize && action != WindowAdjustmentAction.Restore)
            {
                fixture.AssertRefused(action, maximized, WindowAdjustmentRefusal.WindowIsMaximized);
                fixture.AssertRefused(action, minimized, WindowAdjustmentRefusal.WindowIsMinimized);
            }
        }
    }

    /// <summary>Proves leaving closes the mode after any last outcome and requests focus for the captured side.</summary>
    [TestMethod]
    public async Task LeaveWindowAdjustmentWhenAnyPlacementWasLastReadClosesAndReturnsFocusAsync()
    {
        WindowPresenterState[] states =
        [
            WindowPresenterState.Restored,
            WindowPresenterState.Maximized,
            WindowPresenterState.Minimized,
            WindowPresenterState.NotOverlapped,
            WindowPresenterState.Unavailable,
        ];
        foreach (WindowPresenterState state in states)
        {
            using Fixture fixture = await Fixture.ListedAsync();
            _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);
            _ = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);
            _ = fixture.Plan(WindowAdjustmentAction.MoveLeft, WindowAdjustmentSessionTests.Placement(state));
            WindowAdjustmentOpen current = fixture.CurrentOpen();

            WindowAdjustmentState left = fixture.Session.LeaveWindowAdjustment(current);

            WindowAdjustmentClosed closed = Assert.IsInstanceOfType<WindowAdjustmentClosed>(left);
            Assert.AreSame(PaneSide.Right, closed.FileListFocusSide);
            Assert.AreSame(closed, fixture.Session.Current.Scopes.WindowAdjustment);
        }
    }

    /// <summary>Proves after leaving the panes own input again and the mode can be reopened as a new instance.</summary>
    [TestMethod]
    public async Task LeaveWindowAdjustmentWhenModeClosesRestoresPaneInputAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        WindowAdjustmentOpen first = Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            (await fixture.HandleAsync(UserIntent.OpenWindowAdjustment)).Scopes.WindowAdjustment);
        _ = fixture.Session.LeaveWindowAdjustment(first);

        CommanderSnapshot activated = await fixture.HandleAsync(UserIntent.ActivateOtherPane);
        CommanderSnapshot reopened = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);

        Assert.AreSame(PaneSide.Right, activated.Panes.ActiveSide);
        WindowAdjustmentOpen second = Assert.IsInstanceOfType<WindowAdjustmentOpen>(reopened.Scopes.WindowAdjustment);
        Assert.AreNotSame(first, second);
        Assert.AreSame(PaneSide.Right, second.ActiveSide);
    }

    /// <summary>Proves both synchronous members ignore an expected state from an earlier mode instance.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task SynchronousMembersWhenExpectedStateIsStaleAreNoOpsThatKeepTheModeAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        WindowAdjustmentOpen first = Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            (await fixture.HandleAsync(UserIntent.OpenWindowAdjustment)).Scopes.WindowAdjustment);
        _ = fixture.Session.LeaveWindowAdjustment(first);
        WindowAdjustmentOpen second = Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            (await fixture.HandleAsync(UserIntent.OpenWindowAdjustment)).Scopes.WindowAdjustment);

        WindowAdjustmentDecision staleAdjust = fixture.Session.AdjustWindow(WindowAdjustmentRequest.Create(
            first,
            WindowAdjustmentAction.Maximize,
            WindowAdjustmentSessionTests.Placement(WindowPresenterState.Restored)));
        WindowAdjustmentState staleLeave = fixture.Session.LeaveWindowAdjustment(first);

        Assert.AreSame(WindowAdjustmentDecision.NothingToApply, staleAdjust);
        Assert.AreSame(second, staleLeave);
        Assert.AreSame(second, fixture.Session.Current.Scopes.WindowAdjustment);
        Assert.AreSame(WindowAdjustmentOutcome.None, second.Outcome);
    }

    /// <summary>
    /// Proves a window request cannot start a file operation: every action and every operation
    /// intent while the mode is open leaves the operation idle and calls no provider.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task AdjustWindowWhenModeIsOpenCannotStartAFileOperationAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);

        foreach (WindowAdjustmentAction action in WindowAdjustmentPlannerTests.AllActions())
        {
            _ = fixture.Plan(action, WindowAdjustmentSessionTests.Placement(WindowPresenterState.Restored));
        }
        _ = await fixture.HandleAsync(UserIntent.Copy);
        _ = await fixture.HandleAsync(UserIntent.Delete);
        _ = fixture.Session.LeaveWindowAdjustment(fixture.CurrentOpen());

        Assert.AreSame(OperationActivity.Idle, fixture.Session.Current.Panes.Operation);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves a stale window request cannot cancel a running file operation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task SynchronousMembersWhenOperationRunsCannotCancelItAsync()
    {
        DirectoryListing leftListing = Listing("C:\\left", "item.txt");
        BlockingInspectionPort port = BlockingInspectionPort.Create(Inspection(leftListing.Entries[0].Path));
        using Fixture fixture = await Fixture.ListedAsync(port);
        WindowAdjustmentOpen old = Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            (await fixture.HandleAsync(UserIntent.OpenWindowAdjustment)).Scopes.WindowAdjustment);
        _ = fixture.Session.LeaveWindowAdjustment(old);
        Task<CommanderSnapshot> move = fixture.HandleAsync(UserIntent.Move);
        OperationActivity running = fixture.Session.Current.Panes.Operation;

        WindowAdjustmentDecision adjusted = fixture.Session.AdjustWindow(WindowAdjustmentRequest.Create(
            old,
            WindowAdjustmentAction.MoveLeft,
            WindowAdjustmentSessionTests.Placement(WindowPresenterState.Restored)));
        WindowAdjustmentState left = fixture.Session.LeaveWindowAdjustment(old);

        Assert.AreSame(WindowAdjustmentDecision.NothingToApply, adjusted);
        _ = Assert.IsInstanceOfType<WindowAdjustmentClosed>(left);
        Assert.AreSame(running, fixture.Session.Current.Panes.Operation);
        _ = Assert.IsInstanceOfType<OperationRunning>(running);
        CommanderSnapshot completed = await fixture.CompleteBlockedOperationAsync(port, move);
        Assert.AreSame(
            FileOperationCompletionKind.Succeeded,
            Assert.IsInstanceOfType<OperationCompleted>(completed.Panes.Operation).Outcome.Completion);
    }

    /// <summary>Proves a stale window request cannot confirm a permanent deletion awaiting confirmation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task SynchronousMembersWhenDeletionAwaitsConfirmationCannotConfirmItAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        WindowAdjustmentOpen old = Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            (await fixture.HandleAsync(UserIntent.OpenWindowAdjustment)).Scopes.WindowAdjustment);
        _ = fixture.Session.LeaveWindowAdjustment(old);
        OperationActivity awaiting = await fixture.AwaitDecisionAsync("confirmation");

        foreach (WindowAdjustmentAction action in WindowAdjustmentPlannerTests.AllActions())
        {
            Assert.AreSame(WindowAdjustmentDecision.NothingToApply, fixture.Session.AdjustWindow(
                WindowAdjustmentRequest.Create(
                    old,
                    action,
                    WindowAdjustmentSessionTests.Placement(WindowPresenterState.Restored))));
        }
        _ = fixture.Session.LeaveWindowAdjustment(old);
        CommanderSnapshot reopen = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);

        Assert.AreSame(awaiting, reopen.Panes.Operation);
        _ = Assert.IsInstanceOfType<OperationAwaitingConfirmation>(awaiting);
        _ = Assert.IsInstanceOfType<WindowAdjustmentClosed>(reopen.Scopes.WindowAdjustment);
        Assert.HasCount(1, fixture.Port.Calls);
        Assert.StartsWith("Inspect:", fixture.Port.Calls[0]);
    }

    /// <summary>
    /// Proves the two synchronous members, run while a pane read is pending, touch no pane,
    /// settings, address, palette, or operation state and issue no read, so they need no
    /// serialization with the intent path; the completed read then leaves the mode open.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task SynchronousMembersWhenPaneReadIsPendingTouchNoOtherStateAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);
        TaskCompletionSource<DirectoryReadOutcome> read = fixture.Left.EnqueuePending();
        Task<DualPaneSnapshot> background = fixture.Panes.NavigateAsync(
            PaneSide.Left,
            ParsePath("C:\\background"),
            CancellationToken.None);
        CommanderSnapshot before = fixture.Session.Current;
        _ = Assert.IsInstanceOfType<PaneLoading>(before.Panes.Left.Activity);

        foreach (WindowAdjustmentAction action in WindowAdjustmentPlannerTests.AllActions())
        {
            _ = Assert.IsInstanceOfType<WindowAdjustmentPlanned>(fixture.Session.AdjustWindow(
                WindowAdjustmentRequest.Create(
                    fixture.CurrentOpen(),
                    action,
                    WindowAdjustmentSessionTests.Placement(WindowPresenterState.Restored))));
            Fixture.AssertUnchanged(before, fixture.Session.Current);
        }
        WindowAdjustmentOpen beforeLeave = fixture.CurrentOpen();
        read.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\background", "late.txt")));
        _ = await background;
        WindowAdjustmentOpen afterRead = fixture.CurrentOpen();
        WindowAdjustmentState left = fixture.Session.LeaveWindowAdjustment(afterRead);

        Assert.AreSame(beforeLeave, afterRead);
        Assert.HasCount(2, fixture.Left.Requests);
        _ = Assert.IsInstanceOfType<WindowAdjustmentClosed>(left);
        Assert.AreEqual(
            "C:\\background",
            Assert.IsInstanceOfType<PaneContentListed>(fixture.Session.Current.Panes.Left.Content)
                .Listing.Location.CanonicalText);
    }

    /// <summary>Proves a background read that completes, even as a failure, does not end the open scope.</summary>
    [TestMethod]
    public async Task CurrentWhenBackgroundReadCompletesKeepsTheModeOpenAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        WindowAdjustmentOpen open = Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            (await fixture.HandleAsync(UserIntent.OpenWindowAdjustment)).Scopes.WindowAdjustment);
        TaskCompletionSource<DirectoryReadOutcome> read = fixture.Right.EnqueuePending();
        Task<DualPaneSnapshot> background = fixture.Panes.NavigateAsync(
            PaneSide.Right,
            ParsePath("C:\\elsewhere"),
            CancellationToken.None);

        read.SetResult(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
        DualPaneSnapshot completed = await background;

        _ = Assert.IsInstanceOfType<PaneReadFailed>(completed.Right.Activity);
        Assert.AreSame(open, fixture.Session.Current.Scopes.WindowAdjustment);
    }

    private static DirectoryListing Listing(string location, string name)
    {
        DirectoryEntry entry = DirectoryEntry.Create(
            ParsePath(location + "\\" + name),
            name,
            DirectoryEntryKind.File,
            EntryVisibility.Normal);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(
            ParsePath(location),
            [entry],
            DirectoryListingCompleteness.Complete,
            0)).Listing;
    }

    private static FileInspectionOutcome Inspection(FileSystemPath path)
    {
        return InspectionWith(path, DeletionCapability.Recycle);
    }

    private static FileInspectionOutcome InspectionWith(FileSystemPath path, DeletionCapability capability)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(path, identity.Identity, capability));
    }

    private static FileSystemPath ParsePath(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    private sealed class Fixture : System.IDisposable
    {
        private readonly FileOperationGateway _gateway;
        private readonly RecordingCommanderObserver _observer = new();

        private Fixture(IFileOperationPort port, ScriptedFileOperationPort scripted)
        {
            Port = scripted;
            _gateway = new FileOperationGateway(port);
            PaneSession left = new(
                Left,
                Launcher,
                Capacity(),
                DirectoryListing.EntryBoundaryLimit,
                HiddenItemVisibility.Hidden);
            PaneSession right = new(
                Right,
                RightLauncher,
                Capacity(),
                DirectoryListing.EntryBoundaryLimit,
                HiddenItemVisibility.Hidden);
            Panes = new DualPaneSession(left, right, _gateway);
            BookmarkDisplayName name = Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
                BookmarkDisplayName.Parse("Target")).Name;
            BookmarkPath path = Assert.IsInstanceOfType<BookmarkPathAccepted>(
                BookmarkPath.Parse("C:\\bookmark")).Path;
            BookmarkCatalog catalog = Assert.IsInstanceOfType<BookmarkCatalogAccepted>(BookmarkCatalog.Create(
                [],
                [BookmarkEntry.Create(name, path, null, BookmarkShortcutSlot.One)])).Catalog;
            Session = new CommanderSession(
                Panes,
                new SettingsSession(
                    new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
                    SettingsReadOutcome.Read(UserSettings.Create(
                        ColorScheme.NeNeDark,
                        HiddenItemVisibility.Hidden,
                        catalog)),
                    static _ => { }),
                new TransientScopeOwners(
                    new AddressEditorSession(),
                    new CommandPaletteSession(),
                    new WindowAdjustmentSession(),
                    new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog())));
        }

        internal ScriptedDirectoryReadPort Left { get; } = ScriptedDirectoryReadPort.Create();

        internal ScriptedDirectoryReadPort Right { get; } = ScriptedDirectoryReadPort.Create();

        internal ScriptedFileLauncher Launcher { get; } = new();

        internal ScriptedFileLauncher RightLauncher { get; } = new();

        internal ScriptedFileOperationPort Port { get; }

        internal DualPaneSession Panes { get; }

        internal CommanderSession Session { get; }

        internal static Fixture Create()
        {
            ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
            return new Fixture(port, port);
        }

        internal static async Task<Fixture> ListedAsync()
        {
            Fixture fixture = Create();
            await fixture.ListBothAsync();
            return fixture;
        }

        internal static async Task<Fixture> ListedAsync(BlockingInspectionPort port)
        {
            Fixture fixture = new(port, ScriptedFileOperationPort.Create(null, null));
            await fixture.ListBothAsync();
            return fixture;
        }

        internal Task<CommanderSnapshot> HandleAsync(UserIntent intent)
        {
            return Session.HandleAsync(intent, _observer, CancellationToken.None);
        }

        internal ScriptedDirectoryReadPort PortOf(PaneSide side)
        {
            return side == PaneSide.Left ? Left : Right;
        }

        internal ScriptedFileLauncher LauncherOf(PaneSide side)
        {
            return side == PaneSide.Left ? Launcher : RightLauncher;
        }

        internal WindowAdjustmentOpen CurrentOpen()
        {
            return Assert.IsInstanceOfType<WindowAdjustmentOpen>(Session.Current.Scopes.WindowAdjustment);
        }

        internal WindowAdjustmentPlan Plan(WindowAdjustmentAction action, WindowPlacement placement)
        {
            WindowAdjustmentDecision decision = Session.AdjustWindow(
                WindowAdjustmentRequest.Create(CurrentOpen(), action, placement));
            WindowAdjustmentPlan plan = Assert.IsInstanceOfType<WindowAdjustmentPlanned>(decision).Plan;
            AssertOutcomeMatches(action, plan);
            return plan;
        }

        internal WindowBounds Move(WindowAdjustmentAction action, WindowPlacement placement)
        {
            return Assert.IsInstanceOfType<WindowMovePlan>(Plan(action, placement)).Bounds;
        }

        internal WindowBounds Resize(WindowAdjustmentAction action, WindowPlacement placement)
        {
            return Assert.IsInstanceOfType<WindowResizePlan>(Plan(action, placement)).Bounds;
        }

        internal void AssertRefused(
            WindowAdjustmentAction action,
            WindowPlacement placement,
            WindowAdjustmentRefusal expected)
        {
            Assert.AreSame(expected, Assert.IsInstanceOfType<WindowRefusedPlan>(Plan(action, placement)).Reason);
        }

        internal static void AssertUnchanged(CommanderSnapshot before, CommanderSnapshot after)
        {
            Assert.AreSame(before.Panes.Left, after.Panes.Left);
            Assert.AreSame(before.Panes.Right, after.Panes.Right);
            Assert.AreSame(before.Panes.ActiveSide, after.Panes.ActiveSide);
            Assert.AreSame(before.Panes.Operation, after.Panes.Operation);
            Assert.AreSame(before.Settings.Editor, after.Settings.Editor);
            Assert.AreSame(before.Scopes.AddressEditor, after.Scopes.AddressEditor);
            Assert.AreSame(before.Scopes.CommandPalette, after.Scopes.CommandPalette);
        }

        internal async Task<OperationActivity> AwaitDecisionAsync(string decision)
        {
            DirectoryListing leftListing = Listing("C:\\left", "item.txt");
            if (decision == "name")
            {
                return Assert.IsInstanceOfType<OperationAwaitingName>(
                    (await HandleAsync(UserIntent.Rename)).Panes.Operation);
            }
            if (decision == "confirmation")
            {
                Port.EnqueueInspection(InspectionWith(leftListing.Entries[0].Path, DeletionCapability.PermanentOnly));
                return Assert.IsInstanceOfType<OperationAwaitingConfirmation>(
                    (await HandleAsync(UserIntent.Delete)).Panes.Operation);
            }
            FileInspectionSucceeded inspection = Assert.IsInstanceOfType<FileInspectionSucceeded>(
                Inspection(leftListing.Entries[0].Path));
            TransferConflict conflict = TransferConflict.Create(
                inspection.Snapshot,
                ParsePath("C:\\right\\item.txt"),
                ParsePath("C:\\right\\item (2).txt"));
            Port.EnqueueInspection(inspection);
            Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
            return Assert.IsInstanceOfType<OperationAwaitingConflict>(
                (await HandleAsync(UserIntent.Copy)).Panes.Operation);
        }

        internal async Task<CommanderSnapshot> CompleteBlockedOperationAsync(
            BlockingInspectionPort port,
            Task<CommanderSnapshot> operation)
        {
            Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
            Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "item.txt")));
            port.Release();
            return await operation;
        }

        public void Dispose()
        {
            _gateway.Dispose();
        }

        private async Task ListBothAsync()
        {
            Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
            Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "item.txt")));
            _ = await Session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
            _ = await Session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        }

        private void AssertOutcomeMatches(WindowAdjustmentAction action, WindowAdjustmentPlan plan)
        {
            WindowAdjustmentOutcome outcome = CurrentOpen().Outcome;
            if (plan is WindowRefusedPlan refused)
            {
                Assert.AreSame(refused.Reason, Assert.IsInstanceOfType<WindowActionRefused>(outcome).Reason);
                return;
            }
            Assert.AreSame(action, Assert.IsInstanceOfType<WindowActionPlanned>(outcome).Action);
        }

        private static VisiblePageCapacity Capacity()
        {
            return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(4)).Capacity;
        }
    }
}
