using System.Linq;
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

/// <summary>Proves menu selection reaches the existing effects and confirmation paths with its captured targets.</summary>
[TestClass]
public sealed class CommanderContextDispatchTests
{
    /// <summary>Proves transfer submits the selected batch, never an outside focused item, to the captured opposite folder once.</summary>
    [TestMethod]
    [DataRow("copy")]
    [DataRow("move")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenMenuTransfersSelectionUsesExistingGatewayAndOppositePaneAsync(string command)
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await PrepareSelectionAsync(fixture);
        PaneState original = CommanderContextTestContext.StateOf(fixture.Session.Current.Panes.Left);
        foreach (FileSystemPath path in original.Selection)
        {
            fixture.Panes.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(path));
            fixture.Panes.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
            fixture.Panes.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
            fixture.Panes.Port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        }
        fixture.Panes.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        DirectoryListing remaining = command == "move"
            ? DualPaneSessionTestContext.Listing(@"C:\left", ("b.txt", DirectoryEntryKind.File))
            : CommanderContextTestContext.Listing(@"C:\left");
        fixture.Panes.Left.Enqueue(DirectoryReadOutcome.Succeeded(remaining, null));
        fixture.Panes.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing(@"C:\right",
            ("a.txt", DirectoryEntryKind.File), ("c.txt", DirectoryEntryKind.File)), null));
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(await fixture.HandleAsync(UserIntent.OpenContextMenu));
        UserIntent intent = command == "move" ? UserIntent.Move : UserIntent.Copy;

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.SubmitCommand(open, intent));
        _ = await fixture.HandleAsync(UserIntent.SubmitCommand(open, intent));

        Assert.AreSame(FileOperationCompletionKind.Succeeded, Assert.IsInstanceOfType<OperationCompleted>(after.Panes.Operation).Outcome.Completion);
        string[] copies = [@"Copy:C:\left\a.txt", @"Copy:C:\left\c.txt"];
        CollectionAssert.AreEqual(copies, fixture.Panes.Port.Calls.Where(call => call.StartsWith("Copy:", System.StringComparison.Ordinal)).ToArray());
        Assert.AreEqual(@"Preflight:C:\right", fixture.Panes.Port.Calls[2]);
        Assert.AreEqual(command == "move" ? 9 : 7, fixture.Panes.Port.Calls.Count);
        Assert.AreEqual(@"C:\left\b.txt", original.FocusItem?.CanonicalText);
        Assert.AreSame(CommandPaletteState.Closed, after.Scopes.CommandPalette);
    }

    /// <summary>Proves menu deletion preserves the marked batch and requires existing permanent confirmation before effects.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task HandleAsyncWhenMenuDeletesSelectionKeepsExactConfirmationAndCancellationAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await PrepareSelectionAsync(fixture);
        PaneState original = CommanderContextTestContext.StateOf(fixture.Session.Current.Panes.Left);
        foreach (FileSystemPath path in original.Selection)
        {
            fixture.Panes.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(path, DeletionCapability.PermanentOnly));
        }
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(await fixture.HandleAsync(UserIntent.OpenContextMenu));

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.SubmitCommand(open, UserIntent.Delete));
        OperationAwaitingConfirmation confirmation = Assert.IsInstanceOfType<OperationAwaitingConfirmation>(after.Panes.Operation);
        CollectionAssert.AreEqual(original.Selection.ToArray(), confirmation.Request.Sources.ToArray());
        _ = await fixture.FolderAsync(PaneSide.Right);
        Assert.AreSame(confirmation, fixture.Session.Current.Panes.Operation);
        _ = await fixture.HandleAsync(UserIntent.Escape);
        _ = await fixture.HandleAsync(UserIntent.SubmitCommand(open, UserIntent.Delete));

        Assert.HasCount(2, fixture.Panes.Port.Calls);
        Assert.IsTrue(fixture.Panes.Port.Calls.All(call => call.StartsWith("Inspect:", System.StringComparison.Ordinal)));
        Assert.AreSame(CommandPaletteState.Closed, fixture.Session.Current.Scopes.CommandPalette);
    }

    /// <summary>Proves both New actions enter the sole name workflow over the context folder without an eager mutation.</summary>
    [TestMethod]
    [DataRow("directory")]
    [DataRow("file")]
    public async Task HandleAsyncWhenFolderMenuCreatesChildWaitsForExistingNameEntryAsync(string kind)
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(await fixture.FolderAsync(PaneSide.Right));
        UserIntent intent = kind == "file" ? UserIntent.CreateTextFile : UserIntent.CreateDirectory;

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.SubmitCommand(open, intent));

        OperationAwaitingName name = Assert.IsInstanceOfType<OperationAwaitingName>(after.Panes.Operation);
        Assert.AreSame(kind == "file" ? OperationKind.CreateTextFile : OperationKind.CreateDirectory, name.Kind);
        Assert.AreEqual(@"C:\right", name.Subject.CanonicalText);
        Assert.IsEmpty(fixture.Panes.Port.Calls);
        _ = await fixture.HandleAsync(UserIntent.Escape);
        Assert.IsEmpty(fixture.Panes.Port.Calls);
    }

    /// <summary>Proves folder refresh rereads only its accepted pane through the existing query route.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenFolderMenuRefreshesUsesCapturedPaneAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        DirectoryListing updated = DualPaneSessionTestContext.Listing(@"C:\right", ("fresh.txt", DirectoryEntryKind.File));
        fixture.Panes.Right.Enqueue(DirectoryReadOutcome.Succeeded(updated, null));
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(await fixture.FolderAsync(PaneSide.Right));

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.SubmitCommand(open, UserIntent.Refresh));

        Assert.AreSame(updated, Assert.IsInstanceOfType<PaneContentListed>(after.Panes.Right.Content).Listing);
        Assert.HasCount(1, fixture.Panes.Left.Requests);
        Assert.HasCount(2, fixture.Panes.Right.Requests);
        Assert.IsEmpty(fixture.Panes.Port.Calls);
    }

    /// <summary>Proves the global bookmark action opens its existing owner and ignores the departing menu's late cancel.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenFolderMenuOpensBookmarksPreservesNewOwnerAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(await fixture.FolderAsync(PaneSide.Right));
        _ = await fixture.HandleAsync(UserIntent.SubmitCommand(open, UserIntent.OpenBookmarks));
        _ = await fixture.HandleAsync(UserIntent.CancelCommandPalette(open));
        Assert.AreSame(SettingsEditorState.Bookmarks, fixture.Session.Current.Settings.Editor);
        Assert.AreSame(PaneSide.Right, fixture.Session.Current.Panes.ActiveSide);
    }

    private static async Task PrepareSelectionAsync(CommanderContextTestContext fixture)
    {
        await fixture.ListBothAsync();
        _ = await fixture.RowAsync(PaneSide.Left, 0, PaneEntryAction.Select);
        _ = await fixture.RowAsync(PaneSide.Left, 2, PaneEntryAction.ToggleSelection);
        _ = await fixture.HandleAsync(UserIntent.MovePrevious);
    }
}
