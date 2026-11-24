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

/// <summary>Proves captured menu ownership rejects recycled callbacks and out-of-surface dispatch.</summary>
[TestClass]
public sealed class CommanderContextOwnershipTests
{
    /// <summary>Proves equal-path foreign rows and delayed snapshot requests cannot acquire input.</summary>
    [TestMethod]
    [DataRow("foreign")]
    [DataRow("stale")]
    public async Task HandleAsyncWhenContextRequestHasWrongOwnerLeavesPaneAndScopeUnchangedAsync(string source)
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneSnapshot old = fixture.Session.Current.Panes.Right;
        DirectoryEntry entry = source == "foreign"
            ? CommanderContextTestContext.Listing("C:\\right").Entries[0]
            : CommanderContextTestContext.StateOf(old).VisibleEntries[0];
        if (source == "stale")
        {
            _ = await fixture.RowAsync(PaneSide.Right, 1, PaneEntryAction.Select);
        }
        CommanderSnapshot before = fixture.Session.Current;

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.InteractWithPaneEntry(
            PaneSide.Right, old, entry, PaneEntryAction.OpenContextMenu));

        Assert.AreSame(before.Panes.Left, after.Panes.Left);
        Assert.AreSame(before.Panes.Right, after.Panes.Right);
        Assert.AreSame(before.Panes.ActiveSide, after.Panes.ActiveSide);
        Assert.AreSame(before.Scopes.CommandPalette, after.Scopes.CommandPalette);
    }

    /// <summary>Proves a valid replacement captures the new row and refuses both old callback kinds.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenContextIsReplacedRejectsOldCancelAndSubmitAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        CommandPaletteOpen old = CommanderContextTestContext.OpenOf(
            await fixture.RowAsync(PaneSide.Left, 0, PaneEntryAction.OpenContextMenu));

        CommanderSnapshot replaced = await fixture.RowAsync(PaneSide.Right, 2, PaneEntryAction.OpenContextMenu);
        CommandPaletteOpen current = CommanderContextTestContext.OpenOf(replaced);
        _ = await fixture.HandleAsync(UserIntent.CancelCommandPalette(old));
        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.SubmitCommand(old, UserIntent.Delete));

        Assert.AreNotSame(old, current);
        Assert.AreSame(current, after.Scopes.CommandPalette);
        Assert.AreSame(PaneSide.Right, after.Panes.ActiveSide);
        Assert.AreSame(OperationActivity.Idle, after.Panes.Operation);
        Assert.AreSame(replaced.Panes.Right, after.Panes.Right);
    }

    /// <summary>Proves an externally changed endpoint forbids replacement even with a fresh target request.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOldContextEndpointsChangeRefusesReplacementAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(
            await fixture.HandleAsync(UserIntent.OpenContextMenu));
        _ = await fixture.Panes.RightSession.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        CommanderSnapshot before = fixture.Session.Current;

        CommanderSnapshot after = await fixture.FolderAsync(PaneSide.Right);

        Assert.AreSame(open, after.Scopes.CommandPalette);
        Assert.AreSame(before.Panes.Left, after.Panes.Left);
        Assert.AreSame(before.Panes.Right, after.Panes.Right);
        Assert.AreSame(PaneSide.Left, after.Panes.ActiveSide);
    }

    /// <summary>Proves stale pointer admission cannot dismiss a still-valid context owner.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenReplacementPointerIsStaleKeepsCurrentMenuAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneSnapshot stale = fixture.Session.Current.Panes.Left;
        CommanderSnapshot opened = await fixture.HandleAsync(UserIntent.OpenContextMenu);
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(opened);

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.InteractWithPaneSurface(
            PaneSide.Left, stale, PaneSurfaceAction.OpenContextMenu));

        Assert.AreSame(open, after.Scopes.CommandPalette);
        Assert.AreSame(opened.Panes.Left, after.Panes.Left);
    }

    /// <summary>Proves catalog membership alone cannot authorize an absent menu candidate.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenFolderMenuReceivesItemOrPaletteCommandKeepsScopeWithoutDispatchAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        CommanderSnapshot opened = await fixture.FolderAsync(PaneSide.Right);
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(opened);
        UserIntent[] absent = [UserIntent.Delete, UserIntent.Rename, UserIntent.Copy, UserIntent.OpenSettings];

        foreach (UserIntent intent in absent)
        {
            CommanderSnapshot after = await fixture.HandleAsync(UserIntent.SubmitCommand(open, intent));
            Assert.AreSame(open, after.Scopes.CommandPalette);
            Assert.AreSame(opened.Panes.Operation, after.Panes.Operation);
            Assert.AreSame(opened.Settings.Editor, after.Settings.Editor);
            Assert.AreSame(opened.Panes.Left, after.Panes.Left);
            Assert.AreSame(opened.Panes.Right, after.Panes.Right);
        }
    }

    /// <summary>Proves a captured disabled transfer is retained without dispatching or closing.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenContextCandidateIsDisabledDoesNotDispatchAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        DirectoryListing listing = CommanderContextTestContext.Listing("C:\\left");
        fixture.Panes.Left.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        _ = await fixture.Session.NavigateAsync(PaneSide.Left, listing.Location, CancellationToken.None);
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(
            await fixture.HandleAsync(UserIntent.OpenContextMenu));

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.SubmitCommand(open, UserIntent.Copy));

        CommandCandidate candidate = open.Candidates.Single(candidate => candidate.Intent == UserIntent.Copy);
        Assert.AreSame(CommandUnavailableReason.PassivePaneUnavailable,
            Assert.IsInstanceOfType<CommandUnavailable>(candidate.Availability).Reason);
        Assert.AreSame(open, after.Scopes.CommandPalette);
        Assert.AreSame(OperationActivity.Idle, after.Panes.Operation);
    }

    /// <summary>Proves name entry consumes one menu and rejects its later cancellation or second command.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenContextSubmitsRenameConsumesBeforeExistingNameEntryAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(
            await fixture.RowAsync(PaneSide.Right, 2, PaneEntryAction.OpenContextMenu));

        CommanderSnapshot submitted = await fixture.HandleAsync(UserIntent.SubmitCommand(open, UserIntent.Rename));
        _ = await fixture.HandleAsync(UserIntent.CancelCommandPalette(open));
        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.SubmitCommand(open, UserIntent.Delete));

        OperationAwaitingName name = Assert.IsInstanceOfType<OperationAwaitingName>(submitted.Panes.Operation);
        Assert.AreSame(OperationKind.Rename, name.Kind);
        Assert.AreEqual("C:\\right\\c.txt", name.Subject.CanonicalText);
        Assert.AreSame(name, after.Panes.Operation);
        Assert.AreSame(CommandPaletteState.Closed, after.Scopes.CommandPalette);
    }

    /// <summary>Proves current dismissal retains selection and carries only its captured focus side.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCurrentContextCancelsKeepsSelectionAndQualifiesFocusReturnAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        CommanderSnapshot opened = await fixture.RowAsync(PaneSide.Right, 1, PaneEntryAction.OpenContextMenu);
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(opened);

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.CancelCommandPalette(open));

        Assert.AreSame(PaneSide.Right,
            Assert.IsInstanceOfType<CommandPaletteClosed>(after.Scopes.CommandPalette).FileListFocusSide);
        Assert.AreSame(opened.Panes.Right, after.Panes.Right);
        Assert.HasCount(1, CommanderContextTestContext.StateOf(after.Panes.Right).Selection);
    }
    /// <summary>Proves changed endpoints invalidate both dismissal and submission without granting a focus receipt.</summary>
    [TestMethod]
    [DataRow("cancel")]
    [DataRow("submit")]
    public async Task HandleAsyncWhenCapturedEndpointChangesClosesWithoutFocusReceiptAsync(string callback)
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        CommandPaletteOpen open = CommanderContextTestContext.OpenOf(
            await fixture.HandleAsync(UserIntent.OpenContextMenu));
        _ = await fixture.Panes.RightSession.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        CommanderSnapshot before = fixture.Session.Current;
        UserIntent intent = callback == "cancel"
            ? UserIntent.CancelCommandPalette(open)
            : UserIntent.SubmitCommand(open, UserIntent.Rename);

        CommanderSnapshot after = await fixture.HandleAsync(intent);

        Assert.IsNull(Assert.IsInstanceOfType<CommandPaletteClosed>(after.Scopes.CommandPalette).FileListFocusSide);
        Assert.AreSame(before.Panes.Left, after.Panes.Left);
        Assert.AreSame(before.Panes.Right, after.Panes.Right);
        Assert.AreSame(OperationActivity.Idle, after.Panes.Operation);
    }

}
