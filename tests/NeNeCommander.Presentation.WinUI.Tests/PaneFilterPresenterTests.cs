using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves inline editing projections never duplicate query ownership or replay native focus effects.</summary>
[TestClass]
public sealed class PaneFilterPresenterTests
{
    /// <summary>Proves opening requests focus once, while committed text retains the native caret owner.</summary>
    [TestMethod]
    public async Task PresentWhenQueryChangesKeepsExactTextWithoutRepeatedFocusAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PaneFilterPresentation closed = Present(fixture, null);
        _ = await fixture.HandleAsync(UserIntent.OpenPaneFilter);
        PaneFilterPresentation opened = Present(fixture, closed);
        PaneFilterEditing editor = Assert.IsInstanceOfType<PaneFilterEditing>(opened.Editor);

        _ = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(editor, "  a "));
        PaneFilterPresentation changed = Present(fixture, opened);
        PaneFilterPresentation repeated = Present(fixture, changed);

        Assert.IsFalse(closed.IsVisible);
        Assert.IsTrue(opened.FocusEditor);
        Assert.IsTrue(opened.SynchronizeText);
        Assert.IsTrue(changed.IsEditing);
        Assert.IsTrue(changed.IsVisible);
        Assert.IsTrue(changed.IsRestricted);
        Assert.AreEqual("  a ", changed.Text);
        Assert.AreSame(fixture.Session.Current.Panes.Left, changed.SourcePane);
        Assert.AreSame(fixture.Session.Current.Scopes.NameFilter, changed.SourceEditor);
        Assert.IsFalse(changed.FocusEditor);
        Assert.IsFalse(changed.SynchronizeText);
        Assert.IsFalse(repeated.SynchronizeText);
        Assert.IsFalse(repeated.FocusEditor);
        Assert.IsNull(changed.FileListFocusSide);
        Assert.IsNull(repeated.FileListFocusSide);
    }

    /// <summary>Proves explicit Enter keeps the query, returns to the addressed list once, and qualifies the exact state.</summary>
    [TestMethod]
    public async Task QualifyWhenCommitKeepsQueryAndFocusEffectRunsOnceAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PaneFilterPresentation editing = await EditAsync(fixture, "b");
        UserIntent qualified = PaneFilterPresenter.Qualify(UserIntent.Confirm, editing.SourceEditor, editing.Text);
        PaneFilterEditSubmission submission = Assert.IsInstanceOfType<PaneFilterEditSubmission>(qualified);

        _ = await fixture.HandleAsync(qualified);
        PaneFilterPresentation closed = Present(fixture, editing);
        PaneFilterPresentation repeated = Present(fixture, closed);

        Assert.IsTrue(ReferenceEquals(editing.SourceEditor, submission.ExpectedState));
        Assert.AreSame(PaneFilterEditAction.Commit, submission.Action);
        Assert.IsFalse(closed.IsEditing);
        Assert.IsTrue(closed.SynchronizeText);
        Assert.IsTrue(closed.IsVisible);
        Assert.AreEqual("b", closed.Text);
        Assert.AreSame(PaneSide.Left, closed.FileListFocusSide);
        Assert.IsNull(repeated.FileListFocusSide);
        Assert.IsFalse(closed.FocusEditor);
    }

    /// <summary>Proves native blur preserves a restrictive query but never requests file-list focus.</summary>
    [TestMethod]
    public async Task PresentWhenBlurClosesEditorPreservesQueryWithoutFocusAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PaneFilterPresentation editing = await EditAsync(fixture, "b");
        PaneFilterEditing owner = Assert.IsInstanceOfType<PaneFilterEditing>(editing.Editor);

        _ = await fixture.HandleAsync(UserIntent.FinishPaneFilter(owner, PaneFilterEditAction.Blur, owner.Query.Text));
        PaneFilterPresentation blurred = Present(fixture, editing);

        Assert.IsFalse(blurred.IsEditing);
        Assert.IsTrue(blurred.IsVisible);
        Assert.AreEqual("b", blurred.Text);
        Assert.IsNull(blurred.FileListFocusSide);
        Assert.IsFalse(blurred.FocusEditor);
        Assert.AreEqual("PaneFilterAppliedHint", blurred.HintResourceKey);
    }

    /// <summary>Proves Escape clears through the editor's exact state and never directs passive-pane focus.</summary>
    [TestMethod]
    public async Task QualifyWhenCancelClearsQueryWithoutPassiveFocusAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PaneFilterPresentation editing = await EditAsync(fixture, "a");
        PaneFilterPresentation passive = PaneFilterPresenter.Present(fixture.Session.Current, PaneSide.Right, null);
        UserIntent qualified = PaneFilterPresenter.Qualify(UserIntent.Escape, editing.SourceEditor, editing.Text);

        _ = await fixture.HandleAsync(qualified);
        PaneFilterPresentation cancelled = Present(fixture, editing);
        PaneFilterPresentation passiveAfter = PaneFilterPresenter.Present(fixture.Session.Current, PaneSide.Right, passive);

        PaneFilterEditSubmission submission = Assert.IsInstanceOfType<PaneFilterEditSubmission>(qualified);
        Assert.IsTrue(ReferenceEquals(editing.SourceEditor, submission.ExpectedState));
        Assert.AreSame(PaneFilterEditAction.Cancel, submission.Action);
        Assert.IsFalse(cancelled.IsVisible);
        Assert.IsFalse(cancelled.IsRestricted);
        Assert.AreEqual(string.Empty, cancelled.Text);
        Assert.AreSame(PaneSide.Left, cancelled.FileListFocusSide);
        Assert.IsNull(passiveAfter.FileListFocusSide);
        Assert.IsFalse(passiveAfter.FocusEditor);
        Assert.IsFalse(passiveAfter.IsEditing);
    }

    /// <summary>Proves no-match wording describes the applied query rather than declaring the folder empty.</summary>
    [TestMethod]
    public async Task PresentWhenQueryHasNoMatchesUsesExplicitFilterHintAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();

        PaneFilterPresentation filtered = await EditAsync(fixture, "missing");

        Assert.AreEqual("PaneFilterNoMatches", filtered.HintResourceKey);
        Assert.IsTrue(filtered.IsRestricted);
        Assert.IsTrue(filtered.IsVisible);
        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(filtered.SourcePane.Content);
        Assert.IsEmpty(listed.State.VisibleEntries);
        Assert.HasCount(2, listed.Listing.Entries);
    }

    /// <summary>Proves absent content is disabled and no unrelated mapped command becomes an editor exit.</summary>
    [TestMethod]
    public async Task QualifyWhenIntentIsUnrelatedOrOwnerClosedPreservesIntentAsync()
    {
        using PanePointerTestContext fixture = new();
        PaneFilterPresentation unlisted = Present(fixture, null);
        Assert.IsFalse(unlisted.IsEnabled);
        Assert.IsFalse(unlisted.IsVisible);
        Assert.AreEqual(string.Empty, unlisted.Text);
        Assert.AreSame(UserIntent.Confirm, PaneFilterPresenter.Qualify(UserIntent.Confirm, unlisted.SourceEditor, string.Empty));
        await fixture.LoadAsync();
        PaneFilterPresentation editing = await EditAsync(fixture, "a");

        UserIntent unchanged = PaneFilterPresenter.Qualify(UserIntent.Copy, editing.SourceEditor, editing.Text);

        Assert.AreSame(UserIntent.Copy, unchanged);
        Assert.IsTrue(editing.IsEnabled);
    }

    /// <summary>Proves only an external native entry captures blur; native text and exits retain their own route.</summary>
    [TestMethod]
    public async Task CaptureExternalOwnerWhenNativeEntryLeavesEditorCapturesOnlyExactOwnerAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PaneFilterPresentation editing = await EditAsync(fixture, "a");
        PaneFilterEditing owner = Assert.IsInstanceOfType<PaneFilterEditing>(editing.Editor);
        foreach (UserIntent intent in new[] { UserIntent.OpenSettings, UserIntent.OpenBookmarks, UserIntent.OpenHelp,
            UserIntent.OpenCurrentLocationInOtherPane,
            UserIntent.RequestPaneFilter(PaneSide.Left, editing.SourcePane, PaneFilterEntryAction.Clear),
            UserIntent.InteractWithPaneSurface(PaneSide.Left, editing.SourcePane, PaneSurfaceAction.Activate),
            UserIntent.BeginAddressEdit(PaneSide.Left) })
        {
            Assert.AreSame(owner, PaneFilterPresenter.CaptureExternalOwner(intent, editing.SourceEditor));
            Assert.IsNull(PaneFilterPresenter.CaptureExternalOwner(intent, PaneFilterState.Closed));
        }
        foreach (UserIntent intent in new[] { UserIntent.ChangePaneFilterText(owner, "b"),
            UserIntent.FinishPaneFilter(owner, PaneFilterEditAction.Commit, owner.Query.Text),
            UserIntent.FinishPaneFilter(owner, PaneFilterEditAction.Cancel, owner.Query.Text),
            UserIntent.FinishPaneFilter(owner, PaneFilterEditAction.Blur, owner.Query.Text), UserIntent.Copy })
        {
            Assert.IsNull(PaneFilterPresenter.CaptureExternalOwner(intent, editing.SourceEditor));
        }
    }

    private static PaneFilterPresentation Present(PanePointerTestContext fixture, PaneFilterPresentation? previous)
    {
        return PaneFilterPresenter.Present(fixture.Session.Current, PaneSide.Left, previous);
    }

    private static async Task<PaneFilterPresentation> EditAsync(PanePointerTestContext fixture, string text)
    {
        _ = await fixture.HandleAsync(UserIntent.OpenPaneFilter);
        PaneFilterPresentation opened = Present(fixture, null);
        PaneFilterEditing editor = Assert.IsInstanceOfType<PaneFilterEditing>(opened.Editor);
        _ = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(editor, text));
        return Present(fixture, opened);
    }
}
