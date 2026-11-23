using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves direct bookmark slots share pane navigation and its pending-read ownership.</summary>
[TestClass]
public sealed class CommanderSessionBookmarkSlotTests
{
    /// <summary>Proves an assigned fixed slot reads only the active pane through its existing port.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBookmarkSlotIsAssignedNavigatesOnlyTheActivePaneAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "old.txt"), null));
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\bookmark", "new.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        BookmarkCatalog catalog = CommanderSessionTestValues.Catalog(
            CommanderSessionTestValues.Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One));
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession(SettingsReadOutcome.Read(UserSettings.Create(
                DisplayPreferences.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, KeyHintVisibility.Shown, InterfaceLanguage.System),
                catalog, PaneSortPreferences.Default)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);

        CommanderSnapshot navigated = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        Assert.HasCount(2, left.Requests);
        Assert.IsEmpty(right.Requests);
        Assert.AreEqual("C:\\bookmark", left.Requests[1].Location.CanonicalText);
        PaneContentListed content = Assert.IsInstanceOfType<PaneContentListed>(navigated.Panes.Left.Content);
        Assert.AreEqual("C:\\bookmark", content.Listing.Location.CanonicalText);
    }

    /// <summary>Proves an unassigned fixed slot is a metadata no-op with no filesystem read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBookmarkSlotIsUnassignedPerformsNoReadAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        BookmarkCatalog catalog = CommanderSessionTestValues.Catalog(
            CommanderSessionTestValues.Entry("Target", "C:\\target", BookmarkShortcutSlot.One));
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession(SettingsReadOutcome.Read(UserSettings.Create(
                DisplayPreferences.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, KeyHintVisibility.Shown, InterfaceLanguage.System),
                catalog, PaneSortPreferences.Default)));

        CommanderSnapshot unchanged = await session.HandleAsync(
            UserIntent.BookmarkSlotNine,
            new RecordingCommanderObserver(),
            CancellationToken.None);

        Assert.IsEmpty(left.Requests);
        Assert.IsEmpty(right.Requests);
        Assert.AreSame(PaneContent.Absent, unchanged.Panes.Left.Content);
    }

    /// <summary>Proves overlapping direct reads and unrelated intents share one navigation gate.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDirectBookmarkReadIsPendingRejectsOverlappingWorkAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        TaskCompletionSource<DirectoryReadOutcome> read = left.EnqueuePending();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(
            left,
            right,
            gateway,
            CommanderSessionTestValues.Catalog(CommanderSessionTestValues.Entry("Target", "C:\\target", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();

        Task<CommanderSnapshot> navigation = session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);
        await AssertDirectBookmarkOverlapRejectedAsync(session, observer, left, right);
        read.SetResult(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\target", "item.txt"), null));
        _ = await navigation;

        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\target", "next.txt"), null));
        _ = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        Assert.HasCount(2, left.Requests);
    }

    /// <summary>Proves an unrelated left-pane read blocks a direct bookmark read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenLeftPaneReadIsPendingRejectsDirectBookmarkAsync()
    {
        await AssertPendingPaneReadBlocksDirectBookmarkAsync(PaneSide.Left);
    }

    /// <summary>Proves an unrelated right-pane read blocks a direct bookmark read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRightPaneReadIsPendingRejectsDirectBookmarkAsync()
    {
        await AssertPendingPaneReadBlocksDirectBookmarkAsync(PaneSide.Right);
    }

    private static async Task AssertPendingPaneReadBlocksDirectBookmarkAsync(PaneSide side)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort pendingPort = side == PaneSide.Left ? left : right;
        TaskCompletionSource<DirectoryReadOutcome> read = pendingPort.EnqueuePending();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(
            left,
            right,
            gateway,
            CommanderSessionTestValues.Catalog(CommanderSessionTestValues.Entry("Target", "C:\\target", BookmarkShortcutSlot.One)));
        Task<CommanderSnapshot> pending = session.NavigateAsync(
            side,
            CommanderSessionTestValues.ParsePath("C:\\pending"),
            CancellationToken.None);

        _ = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            new RecordingCommanderObserver(),
            CancellationToken.None);

        Assert.HasCount(1, pendingPort.Requests);
        ScriptedDirectoryReadPort otherPort = side == PaneSide.Left ? right : left;
        Assert.IsEmpty(otherPort.Requests);
        read.SetResult(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\pending", "item.txt"), null));
        _ = await pending;
    }

    /// <summary>Proves Escape reaches the pane during a direct bookmark read, abandons it, and releases the bookmark freeze.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDirectBookmarkReadIsPendingEscapeAbandonsItAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "old.txt"), null));
        TaskCompletionSource<DirectoryReadOutcome> read = left.EnqueuePending();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(
            left,
            right,
            gateway,
            CommanderSessionTestValues.Catalog(CommanderSessionTestValues.Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        Task<CommanderSnapshot> bookmark = session.HandleAsync(UserIntent.BookmarkSlotOne, observer, CancellationToken.None);
        CommanderSnapshot frozen = await session.HandleAsync(UserIntent.OpenCommandPalette, observer, CancellationToken.None);

        _ = await session.HandleAsync(UserIntent.Escape, observer, CancellationToken.None);
        CommanderSnapshot returned = await bookmark;
        CommanderSnapshot palette = await session.HandleAsync(UserIntent.OpenCommandPalette, observer, CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, frozen.Scopes.CommandPalette);
        Assert.AreEqual("C:\\bookmark", Assert.IsInstanceOfType<PaneReadAbandoned>(returned.Panes.Left.Activity).Target.CanonicalText);
        Assert.AreEqual("C:\\left", Assert.IsInstanceOfType<PaneContentListed>(returned.Panes.Left.Content).Listing.Location.CanonicalText);
        _ = Assert.IsInstanceOfType<CommandPaletteOpen>(palette.Scopes.CommandPalette);
        Assert.IsFalse(read.Task.IsCompleted);
        read.SetResult(DirectoryReadOutcome.Cancelled());
    }

    /// <summary>Proves a direct bookmark read appends one history location and truncates Forward.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDirectBookmarkSucceedsAppendsOneHistoryLocationAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "old.txt"), null));
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\next", "next.txt"), null));
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "old.txt"), null));
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\bookmark", "new.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(
            left,
            right,
            gateway,
            CommanderSessionTestValues.Catalog(CommanderSessionTestValues.Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\next"), CancellationToken.None);
        CommanderSnapshot back = await session.HandleAsync(
            UserIntent.NavigateBack,
            observer,
            CancellationToken.None);
        PaneNavigationHistory beforeBookmark = CommanderSessionTestValues.HistoryOf(back);
        Assert.HasCount(2, beforeBookmark.Locations);
        Assert.AreEqual(0, beforeBookmark.CurrentIndex);
        Assert.IsNotNull(beforeBookmark.ForwardTarget);

        CommanderSnapshot navigated = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        PaneNavigationHistory history = CommanderSessionTestValues.HistoryOf(navigated);
        Assert.HasCount(2, history.Locations);
        Assert.AreEqual(1, history.CurrentIndex);
        Assert.AreEqual("C:\\left", history.Locations[0].CanonicalText);
        Assert.AreEqual("C:\\bookmark", history.Locations[1].CanonicalText);
        Assert.IsNull(history.ForwardTarget);
    }

    private static async Task AssertDirectBookmarkOverlapRejectedAsync(CommanderSession session, RecordingCommanderObserver observer, ScriptedDirectoryReadPort left, ScriptedDirectoryReadPort right)
    {
        CommanderSnapshot duplicate = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);
        CommanderSnapshot settings = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);
        CommanderSnapshot pane = await session.HandleAsync(
            UserIntent.Refresh,
            observer,
            CancellationToken.None);
        CommanderSnapshot explicitNavigation = await session.NavigateAsync(
            PaneSide.Right,
            CommanderSessionTestValues.ParsePath("C:\\other"),
            CancellationToken.None);

        Assert.HasCount(1, left.Requests);
        Assert.IsEmpty(right.Requests);
        Assert.AreSame(SettingsEditorState.Closed, duplicate.Settings.Editor);
        Assert.AreSame(SettingsEditorState.Closed, settings.Settings.Editor);
        Assert.AreSame(PaneContent.Absent, pane.Panes.Left.Content);
        Assert.AreSame(PaneContent.Absent, explicitNavigation.Panes.Right.Content);
    }
}
