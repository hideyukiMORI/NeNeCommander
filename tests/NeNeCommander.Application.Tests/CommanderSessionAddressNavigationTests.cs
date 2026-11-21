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

/// <summary>Proves address navigation rejects stale submissions and respects pending pane work.</summary>
[TestClass]
public sealed class CommanderSessionAddressNavigationTests
{
    /// <summary>Proves a stale modal dismissal cannot close a newer editor and current dismissal restores its captured list.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenStaleAddressCancellationArrivesKeepsNewEditorAsync()
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
        AddressEditorState oldState = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).Scopes.AddressEditor;
        AddressEditorState newState = (await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Right),
            observer,
            CancellationToken.None)).Scopes.AddressEditor;

        CommanderSnapshot afterCancellation = await session.HandleAsync(
            UserIntent.CancelAddress(oldState),
            observer,
            CancellationToken.None);

        Assert.AreSame(newState, afterCancellation.Scopes.AddressEditor);
        Assert.AreSame(PaneSide.Right, Assert.IsInstanceOfType<AddressEditing>(newState).Side);

        CommanderSnapshot cancelled = await session.HandleAsync(
            UserIntent.CancelAddress(newState),
            observer,
            CancellationToken.None);
        AddressEditorClosed closed = Assert.IsInstanceOfType<AddressEditorClosed>(cancelled.Scopes.AddressEditor);
        Assert.AreSame(PaneSide.Right, closed.FileListFocusSide);
    }

    /// <summary>Proves provider failure and cancellation retain the listed content and selection.</summary>
    [TestMethod]
    [DataRow("failed")]
    [DataRow("cancelled")]
    public async Task HandleAsyncWhenAddressReadDoesNotSucceedPreservesPaneAsync(string outcomeName)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        bool cancelled = outcomeName == "cancelled";
        left.Enqueue(cancelled
            ? DirectoryReadOutcome.Cancelled()
            : DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        PaneContentListed before = Assert.IsInstanceOfType<PaneContentListed>(session.Current.Panes.Left.Content);
        AddressEditorState editing = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).Scopes.AddressEditor;

        CommanderSnapshot completed = await session.HandleAsync(
            UserIntent.SubmitAddress(editing, "C:\\target"),
            observer,
            CancellationToken.None);

        PaneContentListed after = Assert.IsInstanceOfType<PaneContentListed>(completed.Panes.Left.Content);
        Assert.AreSame(before, after);
        Assert.HasCount(1, after.State.Selection);
        if (cancelled)
        {
            _ = Assert.IsInstanceOfType<PaneReadCancelled>(completed.Panes.Left.Activity);
        }
        else
        {
            PaneReadFailed failure = Assert.IsInstanceOfType<PaneReadFailed>(completed.Panes.Left.Activity);
            Assert.AreSame(FileOperationFailureKind.AccessDenied, failure.Failure);
        }
        AddressEditorClosed closed = Assert.IsInstanceOfType<AddressEditorClosed>(completed.Scopes.AddressEditor);
        Assert.AreSame(PaneSide.Left, closed.FileListFocusSide);
    }

    /// <summary>Proves any pane read in flight refuses address entry without changing activation.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaneReadIsRunningRefusesAddressEntryAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        right.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\right", "item.txt"), null));
        TaskCompletionSource<DirectoryReadOutcome> leftRead = left.EnqueuePending();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Right, CommanderSessionTestValues.ParsePath("C:\\right"), CancellationToken.None);
        Task<CommanderSnapshot> loading = session.NavigateAsync(
            PaneSide.Left,
            CommanderSessionTestValues.ParsePath("C:\\loading"),
            CancellationToken.None);

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Right),
            observer,
            CancellationToken.None);

        Assert.AreSame(AddressEditorState.Closed, refused.Scopes.AddressEditor);
        Assert.AreSame(PaneSide.Left, refused.Panes.ActiveSide);
        leftRead.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await loading;
    }

    /// <summary>Proves an operation modal keeps ownership over a requested address editor.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOperationModalOwnsInputRefusesAddressEntryAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.Rename, observer, CancellationToken.None);

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<OperationAwaitingName>(refused.Panes.Operation);
        Assert.AreSame(AddressEditorState.Closed, refused.Scopes.AddressEditor);
    }

    /// <summary>Proves a submission qualified by an old editor cannot navigate either pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenStaleAddressSubmissionArrivesPerformsNoReadAsync()
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
        AddressEditorState oldState = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).Scopes.AddressEditor;
        AddressEditorState newState = (await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Right),
            observer,
            CancellationToken.None)).Scopes.AddressEditor;

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.SubmitAddress(oldState, "C:\\target"),
            observer,
            CancellationToken.None);

        Assert.AreSame(newState, refused.Scopes.AddressEditor);
        Assert.HasCount(1, left.Requests);
        Assert.HasCount(1, right.Requests);
    }

    /// <summary>Proves correction after rejection keeps the captured side and uses normal navigation.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRejectedAddressIsCorrectedNavigatesCapturedPaneAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "old.txt"), null));
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\corrected", "new.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        AddressEditorState editing = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).Scopes.AddressEditor;
        AddressEditorState rejected = (await session.HandleAsync(
            UserIntent.SubmitAddress(editing, "relative"),
            observer,
            CancellationToken.None)).Scopes.AddressEditor;
        CommanderSnapshot repeated = await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Left),
            observer,
            CancellationToken.None);

        CommanderSnapshot corrected = await session.HandleAsync(
            UserIntent.SubmitAddress(rejected, "C:\\corrected"),
            observer,
            CancellationToken.None);

        Assert.AreSame(rejected, repeated.Scopes.AddressEditor);
        PaneContentListed content = Assert.IsInstanceOfType<PaneContentListed>(corrected.Panes.Left.Content);
        Assert.AreEqual("C:\\corrected", content.Listing.Location.CanonicalText);
        Assert.HasCount(2, left.Requests);
    }
}
