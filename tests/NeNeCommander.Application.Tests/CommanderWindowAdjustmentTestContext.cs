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
using NeNeCommander.Application.Windowing;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Owns deterministic panes, scopes, and operation callbacks for Commander window-mode specifications.</summary>
internal sealed class CommanderWindowAdjustmentTestContext : System.IDisposable
{
    private readonly FileOperationGateway _gateway;
    private readonly RecordingCommanderObserver _observer = new();

    private CommanderWindowAdjustmentTestContext(IFileOperationPort port, ScriptedFileOperationPort scripted)
    {
        Port = scripted;
        _gateway = new FileOperationGateway(port);
        PaneSession left = new(Left,
        Launcher,
        PaneSessionConfiguration.Create(Capacity(), DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        PaneSession right = new(Right,
        RightLauncher,
        PaneSessionConfiguration.Create(Capacity(), DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
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
                    catalog, PaneSortPreferences.Default)),
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

    internal ScriptedFileLauncher Launcher { get; } = new();

    internal ScriptedFileLauncher RightLauncher { get; } = new();

    internal ScriptedFileOperationPort Port { get; }

    internal DualPaneSession Panes { get; }

    internal CommanderSession Session { get; }

    internal static CommanderWindowAdjustmentTestContext Create()
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        return new CommanderWindowAdjustmentTestContext(port, port);
    }

    internal static async Task<CommanderWindowAdjustmentTestContext> ListedAsync()
    {
        CommanderWindowAdjustmentTestContext fixture = Create();
        await fixture.ListBothAsync();
        return fixture;
    }

    internal static async Task<CommanderWindowAdjustmentTestContext> ListedAsync(BlockingInspectionPort port)
    {
        CommanderWindowAdjustmentTestContext fixture = new(port, ScriptedFileOperationPort.Create(null, null));
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
        Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt"), null));
        Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "item.txt"), null));
        port.Release();
        return await operation;
    }

    public void Dispose()
    {
        _gateway.Dispose();
    }

    private async Task ListBothAsync()
    {
        Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt"), null));
        Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "item.txt"), null));
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

    internal static DirectoryListing Listing(string location, string name)
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

    internal static FileInspectionOutcome Inspection(FileSystemPath path)
    {
        return InspectionWith(path, DeletionCapability.Recycle);
    }

    internal static FileInspectionOutcome InspectionWith(FileSystemPath path, DeletionCapability capability)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(path, identity.Identity, capability));
    }

    internal static FileSystemPath ParsePath(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
