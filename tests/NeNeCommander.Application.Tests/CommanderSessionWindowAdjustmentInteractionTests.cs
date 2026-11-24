using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves synchronous window adjustment and mode exit cannot change another input owner.</summary>
[TestClass]
public sealed class CommanderSessionWindowAdjustmentInteractionTests
{
    /// <summary>
    /// Proves the open mode freezes every pane, settings, bookmark, address, palette, and operation
    /// intent, and that external navigation is refused without a read.
    /// </summary>
    [TestMethod]
    public async Task HandleAsyncWhenModeIsOpenFreezesEveryIntentAndNavigationAsync()
    {
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
        CommanderSnapshot before = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);
        WindowAdjustmentOpen open = Assert.IsInstanceOfType<WindowAdjustmentOpen>(before.Scopes.WindowAdjustment);
        foreach (UserIntent intent in FrozenIntents())
        {
            CommanderSnapshot frozen = await fixture.HandleAsync(intent);
            CommanderWindowAdjustmentTestContext.AssertUnchanged(before, frozen);
            Assert.AreSame(open, frozen.Scopes.WindowAdjustment);
        }
        CommanderSnapshot navigation = await fixture.Session.NavigateAsync(
            PaneSide.Right,
            CommanderWindowAdjustmentTestContext.ParsePath("C:\\other"),
            CancellationToken.None);

        CommanderWindowAdjustmentTestContext.AssertUnchanged(before, navigation);
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
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
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
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
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
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
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
            using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
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
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
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
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
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
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
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
        DirectoryListing leftListing = CommanderWindowAdjustmentTestContext.Listing("C:\\left", "item.txt");
        BlockingInspectionPort port = BlockingInspectionPort.Create(CommanderWindowAdjustmentTestContext.Inspection(leftListing.Entries[0].Path));
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync(port);
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
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
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
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenWindowAdjustment);
        TaskCompletionSource<DirectoryReadOutcome> read = fixture.Left.EnqueuePending();
        Task<DualPaneSnapshot> background = fixture.Panes.NavigateAsync(
            PaneSide.Left,
            CommanderWindowAdjustmentTestContext.ParsePath("C:\\background"),
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
            CommanderWindowAdjustmentTestContext.AssertUnchanged(before, fixture.Session.Current);
        }
        WindowAdjustmentOpen beforeLeave = fixture.CurrentOpen();
        read.SetResult(DirectoryReadOutcome.Succeeded(CommanderWindowAdjustmentTestContext.Listing("C:\\background", "late.txt"), null));
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
        using CommanderWindowAdjustmentTestContext fixture = await CommanderWindowAdjustmentTestContext.ListedAsync();
        WindowAdjustmentOpen open = Assert.IsInstanceOfType<WindowAdjustmentOpen>(
            (await fixture.HandleAsync(UserIntent.OpenWindowAdjustment)).Scopes.WindowAdjustment);
        TaskCompletionSource<DirectoryReadOutcome> read = fixture.Right.EnqueuePending();
        Task<DualPaneSnapshot> background = fixture.Panes.NavigateAsync(
            PaneSide.Right,
            CommanderWindowAdjustmentTestContext.ParsePath("C:\\elsewhere"),
            CancellationToken.None);

        read.SetResult(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
        DualPaneSnapshot completed = await background;

        _ = Assert.IsInstanceOfType<PaneReadFailed>(completed.Right.Activity);
        Assert.AreSame(open, fixture.Session.Current.Scopes.WindowAdjustment);
    }
    private static UserIntent[] FrozenIntents()
    {
        return
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
            UserIntent.CreateTextFile,
            UserIntent.FocusAddress,
            UserIntent.OpenSettings,
            UserIntent.OpenBookmarks,
            UserIntent.OpenCommandPalette,
            UserIntent.OpenWindowAdjustment,
            UserIntent.BookmarkSlotOne,
        ];
    }

}
