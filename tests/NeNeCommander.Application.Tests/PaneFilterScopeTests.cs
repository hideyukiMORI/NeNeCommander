using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves exact filter ownership, blur and query effects through the real commander dispatcher.</summary>
[TestClass]
public sealed class PaneFilterScopeTests
{
    /// <summary>Supplies the existing modal and transient owners that keep precedence.</summary>
    public static IEnumerable<object[]> HigherOwners()
    {
        UserIntent[] intents = [UserIntent.OpenSettings, UserIntent.OpenColorSchemes,
            UserIntent.OpenBookmarks, UserIntent.FocusAddress, UserIntent.OpenHelp,
            UserIntent.OpenWindowAdjustment, UserIntent.OpenLocations, UserIntent.CreateDirectory];
        return intents.Select(intent => new object[] { intent });
    }

    /// <summary>Proves accepted committed text rebinds only the exact editor to the resulting pane.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task HandleAsyncWhenTextUpdatesRebindsOwnerAndRejectsEveryOldCallback()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneFilterEditing original = EditingOf(await fixture.HandleAsync(UserIntent.OpenPaneFilter));
        CommanderSnapshot changed = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(original, "b.txt"));
        PaneFilterEditing current = EditingOf(changed);

        foreach (UserIntent stale in new[] { UserIntent.ChangePaneFilterText(original, "c.txt"),
            UserIntent.FinishPaneFilter(original, PaneFilterEditAction.Cancel, original.Query.Text),
            UserIntent.FinishPaneFilter(original, PaneFilterEditAction.Commit, original.Query.Text),
            UserIntent.FinishPaneFilter(original, PaneFilterEditAction.Blur, original.Query.Text) })
        {
            CommanderSnapshot ignored = await fixture.HandleAsync(stale);
            Assert.AreSame(current, ignored.Scopes.NameFilter);
            Assert.AreSame(changed.Panes.Left, ignored.Panes.Left);
        }
        Assert.AreNotSame(original, current);
        Assert.AreSame(changed.Panes.Left, current.SourcePane);
        Assert.AreSame(StateOf(changed.Panes.Left).NameFilter, current.Query);
        PaneSessionTestContext.AssertVisibleNames(["b.txt"], StateOf(changed.Panes.Left));
        Assert.HasCount(1, fixture.Panes.Left.Requests);
    }

    /// <summary>Proves native completion keeps query, closes once and cannot close a replacement editor.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task HandleAsyncWhenCommitFinishesEditorKeepsQueryAndRejectsReplay()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneFilterEditing editing = await EditAsync(fixture, "b.txt");

        CommanderSnapshot completed = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Commit, editing.Query.Text));
        CommanderSnapshot repeated = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Commit, editing.Query.Text));
        PaneFilterEditing replacement = EditingOf(await fixture.HandleAsync(UserIntent.OpenPaneFilter));
        CommanderSnapshot lateBlur = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Blur, editing.Query.Text));

        Assert.AreSame(PaneSide.Left, Assert.IsInstanceOfType<PaneFilterClosed>(completed.Scopes.NameFilter).FileListFocusSide);
        Assert.AreSame(completed.Scopes.NameFilter, repeated.Scopes.NameFilter);
        Assert.AreEqual("b.txt", StateOf(completed.Panes.Left).NameFilter.Text);
        Assert.AreSame(replacement, lateBlur.Scopes.NameFilter);
        Assert.IsEmpty(fixture.Panes.Port.Calls);
    }

    /// <summary>Proves blur keeps query and permits subsequent pane handoff without a focus-return effect.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenBlurPrecedesPointerHandoffPreservesDestinationOwner()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneFilterEditing editing = await EditAsync(fixture, "b.txt");
        PaneSnapshot target = fixture.Session.Current.Panes.Right;

        CommanderSnapshot blurred = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Blur, editing.Query.Text));
        CommanderSnapshot entered = await fixture.HandleAsync(UserIntent.RequestPaneFilter(PaneSide.Right, target, PaneFilterEntryAction.Open));
        CommanderSnapshot late = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Blur, editing.Query.Text));

        Assert.IsNull(Assert.IsInstanceOfType<PaneFilterClosed>(blurred.Scopes.NameFilter).FileListFocusSide);
        Assert.AreEqual("b.txt", StateOf(entered.Panes.Left).NameFilter.Text);
        Assert.AreSame(PaneSide.Right, entered.Panes.ActiveSide);
        Assert.AreSame(PaneSide.Right, EditingOf(entered).Side);
        Assert.AreSame(entered.Scopes.NameFilter, late.Scopes.NameFilter);
        Assert.AreSame(entered.Panes.Right, late.Panes.Right);
    }

    /// <summary>Proves cancel clears the query without restoring selection dropped by filtering.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCancelClearsQueryNeverRestoresDroppedMarks()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        _ = await fixture.HandleAsync(UserIntent.SelectAll);
        PaneFilterEditing editing = await EditAsync(fixture, "b.txt");

        CommanderSnapshot cancelled = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Cancel, editing.Query.Text));

        PaneState state = StateOf(cancelled.Panes.Left);
        Assert.IsTrue(state.NameFilter.IsEmpty);
        Assert.HasCount(3, state.VisibleEntries);
        Assert.AreSame(state.VisibleEntries[1].Path, state.Selection.Single());
        Assert.IsNull(state.SelectionAnchor);
        Assert.AreSame(PaneSide.Left, Assert.IsInstanceOfType<PaneFilterClosed>(cancelled.Scopes.NameFilter).FileListFocusSide);
    }

    /// <summary>Proves the explicit clear command uses the same query owner after native blur.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenClearCommandRunsWithoutEditorClearsOnlyActiveQuery()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneFilterEditing editing = await EditAsync(fixture, "b.txt");
        _ = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Blur, editing.Query.Text));
        PaneSnapshot passive = fixture.Session.Current.Panes.Right;

        CommanderSnapshot cleared = await fixture.HandleAsync(UserIntent.ClearPaneFilter);

        Assert.IsTrue(StateOf(cleared.Panes.Left).NameFilter.IsEmpty);
        Assert.AreSame(passive, cleared.Panes.Right);
        Assert.HasCount(1, fixture.Panes.Left.Requests);
        Assert.HasCount(1, fixture.Panes.Right.Requests);
    }

    /// <summary>Proves stale surface captures cannot activate a pane or clear its later query.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSurfaceCaptureIsStaleDoesNotActivateOrClear()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneSnapshot old = fixture.Session.Current.Panes.Left;
        PaneFilterEditing editing = await EditAsync(fixture, "b.txt");
        _ = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Blur, editing.Query.Text));
        CommanderSnapshot before = await fixture.HandleAsync(UserIntent.ActivateOtherPane);

        CommanderSnapshot refused = await fixture.HandleAsync(UserIntent.RequestPaneFilter(PaneSide.Left, old, PaneFilterEntryAction.Clear));

        Assert.AreSame(PaneSide.Right, refused.Panes.ActiveSide);
        Assert.AreSame(before.Panes.Left, refused.Panes.Left);
        Assert.AreSame(before.Scopes.NameFilter, refused.Scopes.NameFilter);
        Assert.AreEqual("b.txt", StateOf(refused.Panes.Left).NameFilter.Text);
    }

    /// <summary>Proves file commands and unqualified navigation do not escape the native editor scope.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task HandleAsyncWhenEditorOwnsInputBlocksLowerCommandsAndExternalNavigation()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneFilterEditing editing = await EditAsync(fixture, "b.txt");
        CommanderSnapshot before = fixture.Session.Current;

        foreach (UserIntent command in new[] { UserIntent.Copy, UserIntent.Delete, UserIntent.SelectAll,
            UserIntent.OpenFocused, UserIntent.ActivateOtherPane, UserIntent.OpenSettings, UserIntent.OpenContextMenu })
        {
            CommanderSnapshot ignored = await fixture.HandleAsync(command);
            Assert.AreSame(editing, ignored.Scopes.NameFilter);
            Assert.AreSame(before.Panes.Left, ignored.Panes.Left);
            Assert.AreSame(PaneSide.Left, ignored.Panes.ActiveSide);
        }
        _ = await fixture.Session.NavigateAsync(PaneSide.Left, PaneSessionTestContext.ParsePath(@"C:\other"), CancellationToken.None);
        Assert.HasCount(1, fixture.Panes.Left.Requests);
        Assert.IsEmpty(fixture.Panes.Port.Calls);
    }

    /// <summary>Proves higher-priority transient/modal owners cannot be replaced by a filter request.</summary>
    [TestMethod]
    [DynamicData(nameof(HigherOwners))]
    public async Task HandleAsyncWhenAnotherOwnerIsOpenKeepsFilterClosed(UserIntent higherOwner)
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        CommanderSnapshot before = await fixture.HandleAsync(higherOwner);

        CommanderSnapshot ignored = await fixture.HandleAsync(UserIntent.OpenPaneFilter);

        Assert.AreSame(before.Scopes.NameFilter, ignored.Scopes.NameFilter);
        _ = Assert.IsInstanceOfType<PaneFilterClosed>(ignored.Scopes.NameFilter);
        Assert.AreSame(before.Panes.Left, ignored.Panes.Left);
        Assert.AreSame(before.Panes.Right, ignored.Panes.Right);
    }

    /// <summary>Proves changed pane ownership rejects query updates, but exact blur can retire without focus.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaneReadSupersedesEditorRejectsTextButAllowsNoFocusBlur()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneFilterEditing editing = await EditAsync(fixture, "b.txt");
        TaskCompletionSource<DirectoryReadOutcome> release = fixture.Panes.Left.EnqueuePendingInline();
        Task<DualPaneSnapshot> reading = fixture.Panes.Panes.NavigateAsync(PaneSide.Left,
            PaneSessionTestContext.ParsePath(@"C:\other"), CancellationToken.None);
        PaneSnapshot pending = fixture.Session.Current.Panes.Left;

        CommanderSnapshot stale = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(editing, "c.txt"));
        CommanderSnapshot blurred = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Blur, "uncommitted-on-old-pane"));
        release.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await reading;

        Assert.AreSame(pending, stale.Panes.Left);
        Assert.AreSame(editing, stale.Scopes.NameFilter);
        Assert.IsNull(Assert.IsInstanceOfType<PaneFilterClosed>(blurred.Scopes.NameFilter).FileListFocusSide);
        Assert.AreEqual("b.txt", StateOf(fixture.Session.Current.Panes.Left).NameFilter.Text);
    }

    /// <summary>Proves an unlisted pane cannot create a filter editing owner.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaneHasNoListingDoesNotOpenFilter()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();

        CommanderSnapshot result = await fixture.HandleAsync(UserIntent.OpenPaneFilter);

        Assert.AreSame(PaneFilterState.Closed, result.Scopes.NameFilter);
        Assert.IsEmpty(fixture.Panes.Left.Requests);
    }

    /// <summary>Proves palette entry uses the same scope, and clearing uses the same pane query mutation.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteOpensAndClearsFilterUsesCanonicalCommandsAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        CommanderSnapshot paletteSnapshot = await fixture.HandleAsync(UserIntent.OpenCommandPalette);
        CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>(paletteSnapshot.Scopes.CommandPalette);
        Assert.AreSame(CommandAvailability.Available,
            palette.Candidates.Single(candidate => candidate.Intent == UserIntent.OpenPaneFilter).Availability);

        CommanderSnapshot opened = await fixture.HandleAsync(UserIntent.SubmitCommand(palette, UserIntent.OpenPaneFilter));
        PaneFilterEditing editor = EditingOf(opened);
        CommanderSnapshot filtered = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(editor, "b.txt"));
        _ = await fixture.HandleAsync(UserIntent.FinishPaneFilter(EditingOf(filtered), PaneFilterEditAction.Blur, EditingOf(filtered).Query.Text));
        CommandPaletteOpen clearPalette = Assert.IsInstanceOfType<CommandPaletteOpen>(
            (await fixture.HandleAsync(UserIntent.OpenCommandPalette)).Scopes.CommandPalette);
        CommanderSnapshot cleared = await fixture.HandleAsync(UserIntent.SubmitCommand(clearPalette, UserIntent.ClearPaneFilter));

        _ = Assert.IsInstanceOfType<CommandPaletteClosed>(opened.Scopes.CommandPalette);
        _ = Assert.IsInstanceOfType<CommandPaletteClosed>(cleared.Scopes.CommandPalette);
        Assert.IsTrue(StateOf(cleared.Panes.Left).NameFilter.IsEmpty);
        Assert.AreSame(PaneSide.Left, Assert.IsInstanceOfType<PaneFilterClosed>(cleared.Scopes.NameFilter).FileListFocusSide);
        Assert.HasCount(1, fixture.Panes.Left.Requests);
        Assert.IsEmpty(fixture.Panes.Port.Calls);
    }

    /// <summary>Proves a current clear-button capture uses the sole pane query transition.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCurrentClearSurfaceIsInvokedClearsTheAddressedQueryAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneFilterEditing editing = await EditAsync(fixture, "b.txt");
        _ = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Blur, editing.Query.Text));
        PaneSnapshot expected = fixture.Session.Current.Panes.Left;

        CommanderSnapshot cleared = await fixture.HandleAsync(UserIntent.RequestPaneFilter(
            PaneSide.Left, expected, PaneFilterEntryAction.Clear));

        Assert.IsTrue(StateOf(cleared.Panes.Left).NameFilter.IsEmpty);
        Assert.AreSame(PaneSide.Left, Assert.IsInstanceOfType<PaneFilterClosed>(cleared.Scopes.NameFilter).FileListFocusSide);
        Assert.HasCount(1, fixture.Panes.Left.Requests);
    }

    /// <summary>Proves a current surface cannot bypass name-entry ownership.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenNameEntryOwnsInputRejectsFilterSurfaceAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        CommanderSnapshot before = await fixture.HandleAsync(UserIntent.CreateDirectory);

        CommanderSnapshot ignored = await fixture.HandleAsync(UserIntent.RequestPaneFilter(
            PaneSide.Right, before.Panes.Right, PaneFilterEntryAction.Open));

        Assert.AreSame(before.Panes.Left, ignored.Panes.Left);
        Assert.AreSame(before.Panes.Right, ignored.Panes.Right);
        Assert.AreSame(before.Panes.ActiveSide, ignored.Panes.ActiveSide);
        Assert.AreSame(before.Panes.Operation, ignored.Panes.Operation);
        Assert.AreSame(before.Scopes.NameFilter, ignored.Scopes.NameFilter);
        Assert.IsEmpty(fixture.Panes.Port.Calls);
    }

    /// <summary>Proves clear cannot fabricate a listing or focus effect before a first read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenClearHasNoListingKeepsTheInitialPaneAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        CommanderSnapshot before = fixture.Session.Current;

        CommanderSnapshot ignored = await fixture.HandleAsync(UserIntent.ClearPaneFilter);

        Assert.AreSame(before.Panes.Left, ignored.Panes.Left);
        Assert.AreSame(PaneFilterState.Closed, ignored.Scopes.NameFilter);
        Assert.IsEmpty(fixture.Panes.Left.Requests);
    }

    private static async Task<PaneFilterEditing> EditAsync(CommanderContextTestContext fixture, string text)
    {
        PaneFilterEditing editing = EditingOf(await fixture.HandleAsync(UserIntent.OpenPaneFilter));
        return EditingOf(await fixture.HandleAsync(UserIntent.ChangePaneFilterText(editing, text)));
    }

    private static PaneFilterEditing EditingOf(CommanderSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneFilterEditing>(snapshot.Scopes.NameFilter);
    }

    private static PaneState StateOf(PaneSnapshot snapshot)
    {
        return CommanderContextTestContext.StateOf(snapshot);
    }
}
