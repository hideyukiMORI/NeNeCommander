using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves actual gateway targets cannot include entries removed by the sole visible filter.</summary>
[TestClass]
public sealed class PaneFilterOperationTests
{
    /// <summary>Proves filtering a marked batch sends only the retained visible source through copy and verification.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenFilterDropsSelectedRowsCopiesOnlyTheVisibleOwnedSourceAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        _ = await fixture.HandleAsync(UserIntent.SelectAll);
        await ApplyAsync(fixture, "b.txt");
        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(fixture.Session.Current.Panes.Left.Content);
        fixture.Panes.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(listed.State.Selection.Single()));
        fixture.Panes.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Panes.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        fixture.Panes.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        fixture.Panes.Left.Enqueue(DirectoryReadOutcome.Succeeded(listed.Listing, null));
        fixture.Panes.Right.Enqueue(DirectoryReadOutcome.Succeeded(CommanderContextTestContext.Listing("C:\\right"), null));

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.Copy);

        string[] expected = ["Inspect:C:\\left\\b.txt", "Preflight:C:\\right", "Copy:C:\\left\\b.txt", "Verify:C:\\left\\b.txt"];
        CollectionAssert.AreEqual(expected, fixture.Panes.Port.Calls.ToArray());
        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(after.Panes.Operation);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Outcome.Completion);
        Assert.AreEqual("b.txt", CommanderContextTestContext.StateOf(after.Panes.Left).NameFilter.Text);
        Assert.IsEmpty(CommanderContextTestContext.StateOf(after.Panes.Left).Selection);
    }

    /// <summary>Proves no-match filtering removes every mark and focus before deletion can inspect a source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenNoFilteredRowsRemainCannotDeleteHiddenFormerSelectionAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        _ = await fixture.HandleAsync(UserIntent.SelectAll);
        await ApplyAsync(fixture, "absent");

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.Delete);

        PaneState state = CommanderContextTestContext.StateOf(after.Panes.Left);
        Assert.IsEmpty(state.VisibleEntries);
        Assert.IsEmpty(state.Selection);
        Assert.IsNull(state.FocusItem);
        Assert.IsEmpty(fixture.Panes.Port.Calls);
        Assert.HasCount(1, fixture.Panes.Left.Requests);
    }

    /// <summary>Proves filter entry and clearing cannot alter a pending permanent-deletion question.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenDeletionOwnsInputRejectsFilterChangesAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        await ApplyAsync(fixture, "b.txt");
        PaneState filtered = CommanderContextTestContext.StateOf(fixture.Session.Current.Panes.Left);
        Assert.IsNotNull(filtered.FocusItem);
        fixture.Panes.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(filtered.FocusItem, DeletionCapability.PermanentOnly));
        CommanderSnapshot before = await fixture.HandleAsync(UserIntent.Delete);
        _ = Assert.IsInstanceOfType<OperationAwaitingConfirmation>(before.Panes.Operation);

        _ = await fixture.HandleAsync(UserIntent.OpenPaneFilter);
        _ = await fixture.HandleAsync(UserIntent.ClearPaneFilter);
        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.RequestPaneFilter(PaneSide.Left,
            before.Panes.Left, PaneFilterEntryAction.Clear));

        Assert.AreSame(before.Panes.Left, after.Panes.Left);
        Assert.AreSame(before.Panes.Operation, after.Panes.Operation);
        Assert.AreSame(before.Scopes.NameFilter, after.Scopes.NameFilter);
        Assert.HasCount(1, fixture.Panes.Port.Calls);
    }

    private static async Task ApplyAsync(CommanderContextTestContext fixture, string text)
    {
        _ = await fixture.HandleAsync(UserIntent.OpenPaneFilter);
        PaneFilterEditing editor = Assert.IsInstanceOfType<PaneFilterEditing>(fixture.Session.Current.Scopes.NameFilter);
        _ = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(editor, text));
        editor = Assert.IsInstanceOfType<PaneFilterEditing>(fixture.Session.Current.Scopes.NameFilter);
        _ = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editor, PaneFilterEditAction.Commit, editor.Query.Text));
    }
}
