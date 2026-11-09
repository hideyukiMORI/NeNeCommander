using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>
/// Proves how the application session admits, freezes and closes the keyboard help (ADR-0076):
/// it opens under the shared idle rule, owns input while open, and closes only through its
/// qualified synchronous route.
/// </summary>
[TestClass]
public sealed class CommanderSessionKeyboardHelpTests
{
    /// <summary>Proves an idle session opens the help on the active side.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenIdleOpensTheHelpOnTheActiveSideAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);

        CommanderSnapshot opened = await fixture.HandleAsync(UserIntent.OpenHelp);

        KeyboardHelpOpen open = Assert.IsInstanceOfType<KeyboardHelpOpen>(opened.Scopes.KeyboardHelp);
        Assert.AreSame(PaneSide.Right, open.ActiveSide);
        Assert.AreSame(open, fixture.Session.Current.Scopes.KeyboardHelp);
    }

    /// <summary>Proves the open settings editor keeps input and refuses the help.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSettingsAreOpenRefusesTheHelpAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenSettings);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenHelp);

        Assert.AreSame(SettingsEditorState.Open, refused.Settings.Editor);
        Assert.AreSame(KeyboardHelpState.Closed, refused.Scopes.KeyboardHelp);
    }

    /// <summary>Proves a file operation awaiting a name refuses the help.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOperationAwaitsANameRefusesTheHelpAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        OperationActivity awaiting = (await fixture.HandleAsync(UserIntent.Rename)).Panes.Operation;

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.OpenHelp);

        _ = Assert.IsInstanceOfType<OperationAwaitingName>(awaiting);
        Assert.AreSame(awaiting, refused.Panes.Operation);
        Assert.AreSame(KeyboardHelpState.Closed, refused.Scopes.KeyboardHelp);
    }

    /// <summary>Proves the palette refuses direct input but a qualified palette submission opens the help.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteSubmitsTheHelpClosesThePaletteAndOpensTheHelpAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>(
            (await fixture.HandleAsync(UserIntent.OpenCommandPalette)).Scopes.CommandPalette);

        CommanderSnapshot direct = await fixture.HandleAsync(UserIntent.OpenHelp);
        CommanderSnapshot submitted = await fixture.HandleAsync(UserIntent.SubmitCommand(palette, UserIntent.OpenHelp));

        Assert.AreSame(palette, direct.Scopes.CommandPalette);
        Assert.AreSame(KeyboardHelpState.Closed, direct.Scopes.KeyboardHelp);
        Assert.AreSame(CommandPaletteState.Closed, submitted.Scopes.CommandPalette);
        _ = Assert.IsInstanceOfType<KeyboardHelpOpen>(submitted.Scopes.KeyboardHelp);
    }

    /// <summary>Proves the open help owns input: pane intents and navigation reach no pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenHelpIsOpenIgnoresEveryIntentAndNavigationAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        CommanderSnapshot before = await fixture.HandleAsync(UserIntent.OpenHelp);
        KeyboardHelpOpen open = Assert.IsInstanceOfType<KeyboardHelpOpen>(before.Scopes.KeyboardHelp);

        CommanderSnapshot moved = await fixture.HandleAsync(UserIntent.MoveNext);
        CommanderSnapshot escaped = await fixture.HandleAsync(UserIntent.Escape);
        CommanderSnapshot settings = await fixture.HandleAsync(UserIntent.OpenSettings);
        CommanderSnapshot navigated = await fixture.Session.NavigateAsync(
            PaneSide.Left,
            ParsePath("C:\\elsewhere"),
            CancellationToken.None);

        foreach (CommanderSnapshot after in new[] { moved, escaped, settings, navigated })
        {
            Assert.AreSame(before.Panes.Left, after.Panes.Left);
            Assert.AreSame(before.Panes.Right, after.Panes.Right);
            Assert.AreSame(SettingsEditorState.Closed, after.Settings.Editor);
            Assert.AreSame(open, after.Scopes.KeyboardHelp);
        }
        Assert.HasCount(1, fixture.Left.Requests);
    }

    /// <summary>Proves the qualified close requests the entry side's focus and reopens the idle routes.</summary>
    [TestMethod]
    public async Task CloseKeyboardHelpWhenExpectedIsCurrentReturnsFocusAndRestoresInputAsync()
    {
        using Fixture fixture = await Fixture.ListedAsync();
        _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);
        KeyboardHelpOpen open = Assert.IsInstanceOfType<KeyboardHelpOpen>(
            (await fixture.HandleAsync(UserIntent.OpenHelp)).Scopes.KeyboardHelp);

        KeyboardHelpState closed = fixture.Session.CloseKeyboardHelp(open);
        CommanderSnapshot settings = await fixture.HandleAsync(UserIntent.OpenSettings);

        Assert.AreSame(PaneSide.Right, Assert.IsInstanceOfType<KeyboardHelpClosed>(closed).FileListFocusSide);
        Assert.AreSame(closed, settings.Scopes.KeyboardHelp);
        Assert.AreSame(SettingsEditorState.Open, settings.Settings.Editor);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fixture.Session.CloseKeyboardHelp(null!));
    }

    private static DirectoryListing Listing(string location, string name)
    {
        DirectoryEntry entry = DirectoryEntry.Create(
            ParsePath(location + "\\" + name),
            name,
            DirectoryEntryKind.File,
            EntryMetadata.Unmeasured(EntryVisibility.Normal));
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(
            ParsePath(location),
            [entry],
            DirectoryListingCompleteness.Complete,
            0)).Listing;
    }

    private static FileSystemPath ParsePath(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly FileOperationGateway _gateway;
        private readonly RecordingCommanderObserver _observer = new();

        private Fixture()
        {
            _gateway = new FileOperationGateway(ScriptedFileOperationPort.Create(null, null));
            PaneSession left = new(
                Left,
                new ScriptedFileLauncher(),
                PaneSessionConfiguration.Create(Capacity(), DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
            PaneSession right = new(
                Right,
                new ScriptedFileLauncher(),
                PaneSessionConfiguration.Create(Capacity(), DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
            Session = new CommanderSession(
                new DualPaneSession(left, right, _gateway),
                new SettingsSession(
                    new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
                    SettingsReadOutcome.Absent(),
                    static _ => { }),
                new TransientScopeOwners(
                    new AddressEditorSession(),
                    new CommandPaletteSession(),
                    new WindowAdjustmentSession(),
                    new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog()),
                    new KeyboardHelpSession()));
        }

        internal ScriptedDirectoryReadPort Left { get; } = ScriptedDirectoryReadPort.Create();

        internal ScriptedDirectoryReadPort Right { get; } = ScriptedDirectoryReadPort.Create();

        internal CommanderSession Session { get; }

        internal static async Task<Fixture> ListedAsync()
        {
            Fixture fixture = new();
            fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt"), null));
            fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "item.txt"), null));
            _ = await fixture.Session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
            _ = await fixture.Session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
            return fixture;
        }

        internal Task<CommanderSnapshot> HandleAsync(UserIntent intent)
        {
            return Session.HandleAsync(intent, _observer, CancellationToken.None);
        }

        public void Dispose()
        {
            _gateway.Dispose();
        }

        private static VisiblePageCapacity Capacity()
        {
            return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(4)).Capacity;
        }
    }
}
