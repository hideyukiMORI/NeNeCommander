using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves bookmark intents respect the active transient scope and pane activity.</summary>
[TestClass]
public sealed class CommanderSessionBookmarkOwnershipTests
{
    /// <summary>Proves an open palette owns input and ignores the manager and every fixed slot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteOwnsInputIgnoresBookmarkIntentsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(
            left,
            right,
            gateway,
            CommanderSessionTestValues.Catalog(CommanderSessionTestValues.Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        CommanderSnapshot opened = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None);
        CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>(opened.Scopes.CommandPalette);

        CommanderSnapshot managerIgnored = await session.HandleAsync(
            UserIntent.OpenBookmarks,
            observer,
            CancellationToken.None);
        CommanderSnapshot slotIgnored = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        Assert.AreSame(palette, managerIgnored.Scopes.CommandPalette);
        Assert.AreSame(palette, slotIgnored.Scopes.CommandPalette);
        Assert.AreSame(SettingsEditorState.Closed, slotIgnored.Settings.Editor);
        Assert.HasCount(1, left.Requests);
        Assert.IsEmpty(right.Requests);
    }

    /// <summary>Proves an open Bookmarks modal ignores palette, address, and settings entry.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBookmarksModalOwnsInputIgnoresPaletteAddressAndSettingsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenBookmarks, observer, CancellationToken.None);

        CommanderSnapshot paletteIgnored = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None);
        CommanderSnapshot shortcutAddressIgnored = await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None);
        CommanderSnapshot nativeAddressIgnored = await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Left),
            observer,
            CancellationToken.None);
        CommanderSnapshot settingsIgnored = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, paletteIgnored.Scopes.CommandPalette);
        Assert.AreSame(AddressEditorState.Closed, shortcutAddressIgnored.Scopes.AddressEditor);
        Assert.AreSame(AddressEditorState.Closed, nativeAddressIgnored.Scopes.AddressEditor);
        Assert.AreSame(SettingsEditorState.Bookmarks, settingsIgnored.Settings.Editor);
        Assert.HasCount(1, left.Requests);
    }

    /// <summary>Proves an active address editor ignores the manager and every fixed slot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenAddressEditorOwnsInputIgnoresBookmarkIntentsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(
            left,
            right,
            gateway,
            CommanderSessionTestValues.Catalog(CommanderSessionTestValues.Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        CommanderSnapshot editing = await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None);
        AddressEditing editor = Assert.IsInstanceOfType<AddressEditing>(editing.Scopes.AddressEditor);

        CommanderSnapshot managerIgnored = await session.HandleAsync(
            UserIntent.OpenBookmarks,
            observer,
            CancellationToken.None);
        CommanderSnapshot slotIgnored = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        Assert.AreSame(editor, managerIgnored.Scopes.AddressEditor);
        Assert.AreSame(editor, slotIgnored.Scopes.AddressEditor);
        Assert.AreEqual("C:\\left", editor.OriginalLocation.CanonicalText);
        Assert.AreSame(SettingsEditorState.Closed, slotIgnored.Settings.Editor);
        Assert.HasCount(1, left.Requests);
    }

    /// <summary>Proves a pending operation modal freezes the bookmark manager and every fixed slot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOperationModalOwnsInputFreezesBookmarkInteractionAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(
            left,
            right,
            gateway,
            CommanderSessionTestValues.Catalog(CommanderSessionTestValues.Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        CommanderSnapshot awaitingName = await session.HandleAsync(
            UserIntent.Rename,
            observer,
            CancellationToken.None);

        CommanderSnapshot managerRefused = await session.HandleAsync(
            UserIntent.OpenBookmarks,
            observer,
            CancellationToken.None);
        CommanderSnapshot slotRefused = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<OperationAwaitingName>(awaitingName.Panes.Operation);
        _ = Assert.IsInstanceOfType<OperationAwaitingName>(slotRefused.Panes.Operation);
        Assert.AreSame(SettingsEditorState.Closed, managerRefused.Settings.Editor);
        Assert.AreSame(SettingsEditorState.Closed, slotRefused.Settings.Editor);
        Assert.HasCount(1, left.Requests);
        Assert.IsEmpty(right.Requests);
    }

    /// <summary>Proves a pending Shell handoff freezes the bookmark manager and every fixed slot.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaneIsLaunchingFreezesBookmarkManagerAndSlotsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedFileLauncher launcher = new();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession(launcher, new ScriptedFileLauncher(), SettingsReadOutcome.Read(UserSettings.Create(
                ColorScheme.NeNeDark,
                HiddenItemVisibility.Hidden,
                CommanderSessionTestValues.Catalog(CommanderSessionTestValues.Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)), PaneSortPreferences.Default)), out _);
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        TaskCompletionSource<Launching.FileLaunchOutcome> completion =
            launcher.EnqueuePending();
        Task<CommanderSnapshot> launching = session.HandleAsync(
            UserIntent.OpenFocused,
            observer,
            CancellationToken.None);

        await AssertLaunchingPaneFreezesBookmarksAsync(session, observer, left);
        completion.SetResult(Launching.FileLaunchOutcome.Accepted());
        _ = await launching;
    }

    /// <summary>Proves a palette submission naming a bookmark slot is rejected without dispatch.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteSubmissionNamesBookmarkSlotRejectsWithoutDispatchAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = CommanderSessionTestValues.CreateSessionWithCatalog(
            left,
            right,
            gateway,
            CommanderSessionTestValues.Catalog(CommanderSessionTestValues.Entry("Target", "C:\\bookmark", BookmarkShortcutSlot.One)));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        CommanderSnapshot opened = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None);
        CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>(opened.Scopes.CommandPalette);

        CommanderSnapshot slotRejected = await session.HandleAsync(
            UserIntent.SubmitCommand(palette, UserIntent.BookmarkSlotOne),
            observer,
            CancellationToken.None);

        Assert.AreSame(palette, slotRejected.Scopes.CommandPalette);
        Assert.AreSame(SettingsEditorState.Closed, slotRejected.Settings.Editor);
        Assert.HasCount(23, CommandCatalog.Commands);
        Assert.IsFalse(CommandCatalog.Commands.Contains(UserIntent.BookmarkSlotOne));
        Assert.HasCount(1, left.Requests);
    }

    private static async Task AssertLaunchingPaneFreezesBookmarksAsync(CommanderSession session, RecordingCommanderObserver observer, ScriptedDirectoryReadPort left)
    {
        CommanderSnapshot managerRefused = await session.HandleAsync(
            UserIntent.OpenBookmarks,
            observer,
            CancellationToken.None);
        CommanderSnapshot slotRefused = await session.HandleAsync(
            UserIntent.BookmarkSlotOne,
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<PaneLaunching>(managerRefused.Panes.Left.Activity);
        Assert.AreSame(SettingsEditorState.Closed, managerRefused.Settings.Editor);
        Assert.AreSame(SettingsEditorState.Closed, slotRefused.Settings.Editor);
        Assert.HasCount(1, left.Requests);
    }
}
