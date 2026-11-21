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

/// <summary>Proves address entry captures a listed pane and preserves its editor ownership.</summary>
[TestClass]
public sealed class CommanderSessionAddressEntryTests
{
    /// <summary>Proves explicit address entry captures the intended listed pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenAddressEditingStartsCapturesAndActivatesRequestedPaneAsync()
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

        CommanderSnapshot leftEditing = await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None);
        CommanderSnapshot repeatedShortcut = await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None);
        CommanderSnapshot repeatedFocus = await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Left),
            observer,
            CancellationToken.None);
        CommanderSnapshot ignoredMovement = await session.HandleAsync(
            UserIntent.MoveNext,
            observer,
            CancellationToken.None);
        CommanderSnapshot rightEditing = await session.HandleAsync(
            UserIntent.BeginAddressEdit(PaneSide.Right),
            observer,
            CancellationToken.None);

        AssertRetainedLeftAddressEditor(leftEditing, repeatedShortcut, repeatedFocus, ignoredMovement);
        AddressEditing rightState = Assert.IsInstanceOfType<AddressEditing>(rightEditing.Scopes.AddressEditor);
        Assert.AreSame(PaneSide.Right, rightState.Side);
        Assert.AreEqual("C:\\right", rightState.OriginalLocation.CanonicalText);
        Assert.AreSame(PaneSide.Right, rightEditing.Panes.ActiveSide);
    }

    /// <summary>Proves invalid raw input remains exact, performs no read, and preserves selection.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenAddressIsInvalidKeepsRawEditorAndPaneContentAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        AddressEditorState editing = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).Scopes.AddressEditor;
        PaneContentListed before = Assert.IsInstanceOfType<PaneContentListed>(session.Current.Panes.Left.Content);
        const string RawText = " C:\\invalid ";

        CommanderSnapshot rejected = await session.HandleAsync(
            UserIntent.SubmitAddress(editing, RawText),
            observer,
            CancellationToken.None);

        AddressInputRejected state = Assert.IsInstanceOfType<AddressInputRejected>(rejected.Scopes.AddressEditor);
        Assert.AreEqual(RawText, state.RawText);
        Assert.AreSame(PathParseFailureKind.Relative, state.Failure);
        Assert.HasCount(1, left.Requests);
        PaneContentListed after = Assert.IsInstanceOfType<PaneContentListed>(rejected.Panes.Left.Content);
        Assert.AreSame(before, after);
        Assert.HasCount(1, after.State.Selection);
    }

    /// <summary>Proves a valid submission closes before reading through the captured pane route.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenAddressIsValidClosesBeforeExistingNavigationCompletesAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "old.txt"), null));
        TaskCompletionSource<DirectoryReadOutcome> targetRead = left.EnqueuePending();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        AddressEditorState editing = (await session.HandleAsync(
            UserIntent.FocusAddress,
            observer,
            CancellationToken.None)).Scopes.AddressEditor;

        Task<CommanderSnapshot> navigating = session.HandleAsync(
            UserIntent.SubmitAddress(editing, "c:/target"),
            observer,
            CancellationToken.None);

        AddressEditorClosed closed = Assert.IsInstanceOfType<AddressEditorClosed>(session.Current.Scopes.AddressEditor);
        Assert.AreSame(PaneSide.Left, closed.FileListFocusSide);
        PaneLoading loading = Assert.IsInstanceOfType<PaneLoading>(session.Current.Panes.Left.Activity);
        Assert.AreEqual("C:\\target", loading.Target.CanonicalText);
        Assert.HasCount(2, left.Requests);
        Assert.AreEqual("C:\\target", left.Requests[1].Location.CanonicalText);
        targetRead.SetResult(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\target", "new.txt"), null));
        CommanderSnapshot navigated = await navigating;
        PaneContentListed content = Assert.IsInstanceOfType<PaneContentListed>(navigated.Panes.Left.Content);
        Assert.AreEqual("C:\\target", content.Listing.Location.CanonicalText);
    }

    /// <summary>Proves Escape closes address editing without reaching pane selection reduction.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenEscapeCancelsAddressPreservesSelectionAndRequestsListFocusAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.FocusAddress, observer, CancellationToken.None);

        CommanderSnapshot cancelled = await session.HandleAsync(
            UserIntent.Escape,
            observer,
            CancellationToken.None);

        AddressEditorClosed closed = Assert.IsInstanceOfType<AddressEditorClosed>(cancelled.Scopes.AddressEditor);
        Assert.AreSame(PaneSide.Left, closed.FileListFocusSide);
        PaneContentListed content = Assert.IsInstanceOfType<PaneContentListed>(cancelled.Panes.Left.Content);
        Assert.AreEqual("C:\\left", content.Listing.Location.CanonicalText);
        Assert.HasCount(1, content.State.Selection);
    }

    /// <summary>Proves an address editor cannot start before its pane has listed a location.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenActivePaneHasNoListingRefusesAddressEntryAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.FocusAddress,
            new RecordingCommanderObserver(),
            CancellationToken.None);

        Assert.AreSame(AddressEditorState.Closed, refused.Scopes.AddressEditor);
        Assert.IsEmpty(left.Requests);
        Assert.IsEmpty(right.Requests);
    }

    private static void AssertRetainedLeftAddressEditor(CommanderSnapshot leftEditing, CommanderSnapshot repeatedShortcut, CommanderSnapshot repeatedFocus, CommanderSnapshot ignoredMovement)
    {
        AddressEditing leftState = Assert.IsInstanceOfType<AddressEditing>(leftEditing.Scopes.AddressEditor);
        Assert.AreSame(leftState, repeatedShortcut.Scopes.AddressEditor);
        Assert.AreSame(leftState, repeatedFocus.Scopes.AddressEditor);
        Assert.AreSame(leftState, ignoredMovement.Scopes.AddressEditor);
        Assert.AreSame(PaneSide.Left, leftState.Side);
        Assert.AreEqual("C:\\left", leftState.OriginalLocation.CanonicalText);
    }
}
