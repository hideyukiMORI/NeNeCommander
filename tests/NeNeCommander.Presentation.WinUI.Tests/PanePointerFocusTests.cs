using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves only admitted synchronous pane input requests native focus return.</summary>
[TestClass]
public sealed class PanePointerFocusTests
{
    /// <summary>Proves listed headers retain model identity while returning their accepted side.</summary>
    [TestMethod]
    public async Task ObserveResultWhenListedHeaderAcceptedReturnsSideAndRejectsReplayAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        await AssertHeaderReceiptAsync(fixture);
    }

    /// <summary>Proves unlisted headers retain absent content while returning their accepted side.</summary>
    [TestMethod]
    public async Task ObserveResultWhenUnlistedHeaderAcceptedReturnsSideAndRejectsReplayAsync()
    {
        using PanePointerTestContext fixture = new();
        await AssertHeaderReceiptAsync(fixture);
    }

    private static async Task AssertHeaderReceiptAsync(PanePointerTestContext fixture)
    {
        PanePointerMapper mapper = new();
        CommanderSnapshot before = fixture.Session.Current;
        UserIntent intent = UserIntent.InteractWithPaneSurface(PaneSide.Right, before.Panes.Right, PaneSurfaceAction.Activate);

        CommanderSnapshot after = await fixture.HandleAsync(intent);

        Assert.AreSame(PaneSide.Right, mapper.ObserveResult(intent, before, after));
        Assert.AreNotSame(before.Panes.Right, after.Panes.Right);
        Assert.AreSame(before.Panes.Right.Content, after.Panes.Right.Content);
        Assert.AreSame(before.Panes.Right.Activity, after.Panes.Right.Activity);
        CommanderSnapshot replay = await fixture.HandleAsync(intent);
        Assert.AreSame(after.Panes.Right, replay.Panes.Right);
        Assert.IsNull(mapper.ObserveResult(intent, after, replay));
    }

    /// <summary>Proves empty background actions also provide a receipt without inventing content.</summary>
    [TestMethod]
    public async Task ObserveResultWhenUnlistedBlankAcceptedReturnsSideAsync()
    {
        using PanePointerTestContext fixture = new();
        PanePointerMapper mapper = new();
        CommanderSnapshot before = fixture.Session.Current;
        UserIntent intent = UserIntent.InteractWithPaneSurface(PaneSide.Left, before.Panes.Left, PaneSurfaceAction.ClearSelection);

        CommanderSnapshot after = await fixture.HandleAsync(intent);

        Assert.AreSame(PaneSide.Left, mapper.ObserveResult(intent, before, after));
        Assert.AreSame(before.Panes.Left.Content, after.Panes.Left.Content);
    }

    /// <summary>Proves admitted row selection returns its pane and retains the existing double-tap receipt.</summary>
    [TestMethod]
    public async Task ObserveResultWhenSelectionAcceptedReturnsSideAndSeedsDoubleTapAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePointerMapper mapper = new();
        PanePresentation pane = fixture.PresentRight();
        UserIntent intent = Assert.IsInstanceOfType<PaneRowInteraction>(mapper.MapRowTap(PaneSide.Right, pane, pane.Rows[1], PanePointerModifier.None));
        CommanderSnapshot before = fixture.Session.Current;

        CommanderSnapshot after = await fixture.HandleAsync(intent);

        Assert.AreSame(PaneSide.Right, mapper.ObserveResult(intent, before, after));
        pane = fixture.PresentRight();
        Assert.IsNotNull(mapper.MapRowDoubleTap(PaneSide.Right, pane, pane.Rows[1], PanePointerModifier.None));
    }

    /// <summary>Proves stale requests with the same current focus cannot request native focus.</summary>
    [TestMethod]
    public async Task ObserveResultWhenStaleSameFocusReturnsNoSideAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);
        PanePointerMapper mapper = new();
        PanePresentation pane = fixture.PresentRight();
        UserIntent intent = Assert.IsInstanceOfType<PaneRowInteraction>(mapper.MapRowTap(PaneSide.Right, pane, pane.Rows[0], PanePointerModifier.None));
        _ = await fixture.HandleAsync(UserIntent.ToggleSelection);
        CommanderSnapshot before = fixture.Session.Current;

        CommanderSnapshot after = await fixture.HandleAsync(intent);

        Assert.IsNull(mapper.ObserveResult(intent, before, after));
    }

    /// <summary>Proves modal ownership cannot be displaced by a current header request.</summary>
    [TestMethod]
    public async Task ObserveResultWhenSettingsOwnInputReturnsNoSideAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenSettings);
        PanePointerMapper mapper = new();
        CommanderSnapshot before = fixture.Session.Current;
        UserIntent intent = UserIntent.InteractWithPaneSurface(PaneSide.Right, before.Panes.Right, PaneSurfaceAction.Activate);

        CommanderSnapshot after = await fixture.HandleAsync(intent);

        Assert.IsNull(mapper.ObserveResult(intent, before, after));
        Assert.AreSame(before.Panes.Right, after.Panes.Right);
    }

    /// <summary>Proves asynchronous opening never shares the synchronous focus-return receipt.</summary>
    [TestMethod]
    public async Task ObserveResultWhenOpenCompletesReturnsNoSideAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePointerMapper mapper = new();
        PanePresentation pane = fixture.PresentRight();
        CommanderSnapshot before = fixture.Session.Current;
        UserIntent intent = UserIntent.InteractWithPaneEntry(PaneSide.Right, pane.SourceSnapshot, pane.Rows[0].Entry, PaneEntryAction.Open);
        DirectoryListing child = DualPaneTestContext.CreateListing(pane.Rows[0].Entry.Path.CanonicalText, []);
        TaskCompletionSource<DirectoryReadOutcome> completion = fixture.Right.EnqueuePending();

        Task<CommanderSnapshot> work = fixture.HandleAsync(intent);
        Assert.IsFalse(work.IsCompleted);
        completion.SetResult(DirectoryReadOutcome.Succeeded(child, null));
        CommanderSnapshot after = await work;

        Assert.AreNotSame(before.Panes.Right, after.Panes.Right);
        Assert.IsNull(mapper.ObserveResult(intent, before, after));
    }
}
