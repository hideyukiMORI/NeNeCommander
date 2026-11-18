using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves palette admission and execution remain bound to their captured pane scope.</summary>
[TestClass]
public sealed class CommanderSessionPaletteOwnershipTests
{
    /// <summary>Proves an old Enter event cannot close or execute a newly opened palette.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOldPaletteSubmissionArrivesKeepsCurrentPaletteAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        CommandPaletteOpen old = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);
        _ = await session.HandleAsync(UserIntent.CancelCommandPalette(old), observer, CancellationToken.None);
        CommandPaletteOpen current = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);

        CommanderSnapshot staleCancel = await session.HandleAsync(
            UserIntent.CancelCommandPalette(old),
            observer,
            CancellationToken.None);
        CommanderSnapshot ignored = await session.HandleAsync(
            UserIntent.SubmitCommand(old, UserIntent.Rename),
            observer,
            CancellationToken.None);

        Assert.AreSame(current, staleCancel.Scopes.CommandPalette);
        Assert.AreSame(current, ignored.Scopes.CommandPalette);
        Assert.AreSame(OperationActivity.Idle, ignored.Panes.Operation);
    }

    /// <summary>Proves active-side and passive-snapshot changes fail closed without old-side focus.</summary>
    [TestMethod]
    [DataRow("active")]
    [DataRow("passive")]
    public async Task HandleAsyncWhenCapturedPaletteScopeChangesRejectsExecutionAsync(string changedPart)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "left.txt"), null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\right", "right.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession(out DualPaneSession panes);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, CommanderSessionTestValues.ParsePath("C:\\right"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);
        _ = await panes.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        if (changedPart == "passive")
        {
            _ = await panes.HandleAsync(UserIntent.ToggleHiddenItems, observer, CancellationToken.None);
            _ = await panes.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        }

        CommanderSnapshot rejected = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.Copy),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, rejected.Scopes.CommandPalette);
        Assert.AreSame(changedPart == "active" ? PaneSide.Right : PaneSide.Left, rejected.Panes.ActiveSide);
        Assert.AreSame(OperationActivity.Idle, rejected.Panes.Operation);
    }

    /// <summary>Proves stale Escape closes without overriding a newer modal's focus ownership.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteScopeGainsModalEscapeDoesNotRestoreOldPaneFocusAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession(out DualPaneSession panes);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);
        _ = await panes.HandleAsync(UserIntent.Rename, observer, CancellationToken.None);

        CommanderSnapshot closed = await session.HandleAsync(
            UserIntent.CancelCommandPalette(open),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, closed.Scopes.CommandPalette);
        _ = Assert.IsInstanceOfType<OperationAwaitingName>(closed.Panes.Operation);
    }

    /// <summary>Proves pane reads and file launches in flight refuse palette capture.</summary>
    [TestMethod]
    [DataRow("loading")]
    [DataRow("launching")]
    public async Task HandleAsyncWhenPaneExternalWorkRunsRefusesPaletteAsync(string activity)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedFileLauncher launcher = new();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession(launcher);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        Task<CommanderSnapshot> pending;
        if (activity == "loading")
        {
            TaskCompletionSource<DirectoryReadOutcome> completion = left.EnqueuePending();
            pending = session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\target"), CancellationToken.None);
            CommanderSnapshot refused = await session.HandleAsync(
                UserIntent.OpenCommandPalette,
                observer,
                CancellationToken.None);
            Assert.AreSame(CommandPaletteState.Closed, refused.Scopes.CommandPalette);
            _ = Assert.IsInstanceOfType<PaneLoading>(refused.Panes.Left.Activity);
            completion.SetResult(DirectoryReadOutcome.Cancelled());
        }
        else
        {
            TaskCompletionSource<Launching.FileLaunchOutcome> completion =
                launcher.EnqueuePending();
            pending = session.HandleAsync(UserIntent.OpenFocused, observer, CancellationToken.None);
            CommanderSnapshot refused = await session.HandleAsync(
                UserIntent.OpenCommandPalette,
                observer,
                CancellationToken.None);
            Assert.AreSame(CommandPaletteState.Closed, refused.Scopes.CommandPalette);
            _ = Assert.IsInstanceOfType<PaneLaunching>(refused.Panes.Left.Activity);
            completion.SetResult(Launching.FileLaunchOutcome.Accepted());
        }
        _ = await pending;
    }

    /// <summary>Proves each scope a loading pane refuses admits again once Escape abandons the read, with no new predicate.</summary>
    [TestMethod]
    [DataRow("palette")]
    [DataRow("bookmarks")]
    [DataRow("address")]
    [DataRow("window")]
    public async Task HandleAsyncWhenLoadingReadIsAbandonedAdmitsScopeAgainAsync(string scope)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        TaskCompletionSource<DirectoryReadOutcome> completion = left.EnqueuePending();
        Task<CommanderSnapshot> pending = session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\unreachable"), CancellationToken.None);
        UserIntent opening = scope switch
        {
            "palette" => UserIntent.OpenCommandPalette,
            "bookmarks" => UserIntent.OpenBookmarks,
            "address" => UserIntent.FocusAddress,
            _ => UserIntent.OpenWindowAdjustment,
        };
        CommanderSnapshot refused = await session.HandleAsync(opening, observer, CancellationToken.None);

        CommanderSnapshot abandoned = await session.HandleAsync(UserIntent.Escape, observer, CancellationToken.None);
        CommanderSnapshot admitted = await session.HandleAsync(opening, observer, CancellationToken.None);

        Assert.IsFalse(ScopeIsOpen(refused, scope));
        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(abandoned.Panes.Left.Activity);
        Assert.IsTrue(ScopeIsOpen(admitted, scope));
        completion.SetResult(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\unreachable", "late.txt"), null));
        CommanderSnapshot late = await pending;
        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(late.Panes.Left.Activity);
        Assert.AreEqual("C:\\left", Assert.IsInstanceOfType<PaneContentListed>(late.Panes.Left.Content).Listing.Location.CanonicalText);
    }

    private static bool ScopeIsOpen(CommanderSnapshot snapshot, string scope)
    {
        return scope switch
        {
            "palette" => snapshot.Scopes.CommandPalette is CommandPaletteOpen,
            "bookmarks" => snapshot.Settings.Editor == SettingsEditorState.Bookmarks,
            "address" => snapshot.Scopes.AddressEditor is AddressEditing,
            _ => snapshot.Scopes.WindowAdjustment is WindowAdjustmentOpen,
        };
    }

    /// <summary>Proves external work in the right pane independently blocks palette capture.</summary>
    [TestMethod]
    [DataRow("loading")]
    [DataRow("launching")]
    public async Task HandleAsyncWhenRightPaneExternalWorkRunsRefusesPaletteAsync(string activity)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedFileLauncher leftLauncher = new();
        ScriptedFileLauncher rightLauncher = new();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "left.txt"), null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\right", "right.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession(leftLauncher, rightLauncher, out _);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, CommanderSessionTestValues.ParsePath("C:\\right"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        Task<CommanderSnapshot> pending;
        if (activity == "loading")
        {
            TaskCompletionSource<DirectoryReadOutcome> completion = right.EnqueuePending();
            pending = session.NavigateAsync(PaneSide.Right, CommanderSessionTestValues.ParsePath("C:\\target"), CancellationToken.None);
            await AssertRightPaneLoadingRefusesPaletteAsync(session, observer);
            completion.SetResult(DirectoryReadOutcome.Cancelled());
        }
        else
        {
            TaskCompletionSource<Launching.FileLaunchOutcome> completion =
                rightLauncher.EnqueuePending();
            pending = session.HandleAsync(UserIntent.OpenFocused, observer, CancellationToken.None);
            await AssertRightPaneLaunchingRefusesPaletteAsync(session, observer);
            completion.SetResult(Launching.FileLaunchOutcome.Accepted());
        }
        _ = await pending;
    }

    private static async Task AssertRightPaneLoadingRefusesPaletteAsync(CommanderSession session, RecordingCommanderObserver observer)
    {
        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None);
        Assert.AreSame(CommandPaletteState.Closed, refused.Scopes.CommandPalette);
        _ = Assert.IsInstanceOfType<PaneLoading>(refused.Panes.Right.Activity);
    }

    private static async Task AssertRightPaneLaunchingRefusesPaletteAsync(CommanderSession session, RecordingCommanderObserver observer)
    {
        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None);
        Assert.AreSame(CommandPaletteState.Closed, refused.Scopes.CommandPalette);
        _ = Assert.IsInstanceOfType<PaneLaunching>(refused.Panes.Right.Activity);
    }
}
