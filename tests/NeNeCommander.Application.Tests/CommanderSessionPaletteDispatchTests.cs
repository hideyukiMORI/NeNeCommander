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

/// <summary>Proves accepted palette commands use the existing intent and scope owners.</summary>
[TestClass]
public sealed class CommanderSessionPaletteDispatchTests
{
    /// <summary>Proves an available palette command closes and enters the existing modal route once.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteCommandIsAvailableDispatchesExistingIntentAsync()
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

        CommanderSnapshot dispatched = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.Rename),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, dispatched.Scopes.CommandPalette);
        OperationAwaitingName awaiting = Assert.IsInstanceOfType<OperationAwaitingName>(dispatched.Panes.Operation);
        Assert.AreSame(OperationKind.Rename, awaiting.Kind);
        Assert.AreEqual("C:\\left\\item.txt", awaiting.Subject.CanonicalText);
    }

    /// <summary>Proves both sort commands are available and a palette submission reaches the active pane once.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteSubmitsSortCommandReordersActivePaneAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, store).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);

        CommanderSnapshot dispatched = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.SortByExtension),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandAvailability.Available, CommanderSessionTestValues.Candidate(open, UserIntent.SortByName).Availability);
        Assert.AreSame(CommandAvailability.Available, CommanderSessionTestValues.Candidate(open, UserIntent.SortByExtension).Availability);
        Assert.AreSame(CommandPaletteState.Closed, dispatched.Scopes.CommandPalette);
        PaneSortOrder order = Assert.IsInstanceOfType<PaneContentListed>(dispatched.Panes.Left.Content).State.SortOrder;
        Assert.AreSame(SortKey.Extension, order.Key);
        Assert.AreSame(SortDirection.Ascending, order.Direction);
        Assert.HasCount(1, left.Requests);
        Assert.HasCount(1, store.Writes);
        Assert.AreEqual(order, store.Writes[0].PaneSortOrders.Left);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await session.StopAsync(null, new RecordingCommanderObserver());
    }

    /// <summary>Proves settings, the palette, and the window mode keep a sort intent away from the panes.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenScopeOwnsInputIgnoresSortIntentAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);

        _ = await session.HandleAsync(UserIntent.OpenSettings, observer, CancellationToken.None);
        CommanderSnapshot inSettings = await session.HandleAsync(UserIntent.SortByName, observer, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.Escape, observer, CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);
        CommanderSnapshot inPalette = await session.HandleAsync(UserIntent.SortByName, observer, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.CancelCommandPalette(open), observer, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenWindowAdjustment, observer, CancellationToken.None);
        CommanderSnapshot inWindowMode = await session.HandleAsync(UserIntent.SortByExtension, observer, CancellationToken.None);

        Assert.AreSame(SettingsEditorState.Open, inSettings.Settings.Editor);
        Assert.AreSame(open, inPalette.Scopes.CommandPalette);
        _ = Assert.IsInstanceOfType<WindowAdjustmentOpen>(inWindowMode.Scopes.WindowAdjustment);
        foreach (CommanderSnapshot snapshot in new[] { inSettings, inPalette, inWindowMode })
        {
            Assert.AreEqual(
                PaneSortOrder.Default,
                Assert.IsInstanceOfType<PaneContentListed>(snapshot.Panes.Left.Content).State.SortOrder);
        }
    }

    /// <summary>Proves settings and address commands transfer ownership to their existing editors.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteSubmitsEditorCommandsTransfersExistingOwnerAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        CommandPaletteOpen settingsPalette = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);

        CommanderSnapshot settings = await session.HandleAsync(
            UserIntent.SubmitCommand(settingsPalette, UserIntent.OpenSettings),
            observer,
            CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.Escape, observer, CancellationToken.None);
        CommandPaletteOpen addressPalette = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);
        CommanderSnapshot address = await session.HandleAsync(
            UserIntent.SubmitCommand(addressPalette, UserIntent.FocusAddress),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, settings.Scopes.CommandPalette);
        Assert.AreSame(SettingsEditorState.Open, settings.Settings.Editor);
        Assert.AreSame(CommandPaletteState.Closed, address.Scopes.CommandPalette);
        AddressEditing editing = Assert.IsInstanceOfType<AddressEditing>(address.Scopes.AddressEditor);
        Assert.AreSame(PaneSide.Left, editing.Side);
    }

    /// <summary>Proves pane activation executes once and leaves the new side authoritative.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteActivatesOtherPaneKeepsNewSideAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "left.txt"), null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\right", "right.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, CommanderSessionTestValues.ParsePath("C:\\right"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);

        CommanderSnapshot activated = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.ActivateOtherPane),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, activated.Scopes.CommandPalette);
        Assert.AreSame(PaneSide.Right, activated.Panes.ActiveSide);
        Assert.AreSame(open.Left, activated.Panes.Left);
        Assert.AreSame(open.Right, activated.Panes.Right);
    }

    /// <summary>Proves palette Delete enters the existing confirmation with its captured source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task HandleAsyncWhenPaletteSubmitsDeletePreservesConfirmationTargetAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = CommanderSessionTestValues.Listing("C:\\left", "item.txt");
        left.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(CommanderSessionTestValues.PermanentInspection(listing.Entries[0].Path));
        using FileOperationGateway gateway = new(port);
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, listing.Location, CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);

        CommanderSnapshot dispatched = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.Delete),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, dispatched.Scopes.CommandPalette);
        OperationAwaitingConfirmation awaiting =
            Assert.IsInstanceOfType<OperationAwaitingConfirmation>(dispatched.Panes.Operation);
        Assert.HasCount(1, awaiting.Request.Sources);
        Assert.AreSame(listing.Entries[0].Path, awaiting.Request.Sources[0]);
        Assert.HasCount(1, port.Calls);
        Assert.AreEqual("Inspect:C:\\left\\item.txt", port.Calls[0]);
    }
}
