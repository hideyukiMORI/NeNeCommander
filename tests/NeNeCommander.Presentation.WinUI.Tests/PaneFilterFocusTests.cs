using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves explicit filter exits retain exact ownership until native focus is applied.</summary>
[TestClass]
public sealed class PaneFilterFocusTests
{
    /// <summary>Proves an Enter exit may leave its old textbox once, without stealing unrelated native text.</summary>
    [TestMethod]
    public async Task FocusTargetWhenExactCommitStillOwnsTextBoxReturnsItsPaneAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PaneFilterClosed closed = await CommitAsync(fixture);

        foreach (KeyboardContext context in new[] { KeyboardContext.PaneFilter, KeyboardContext.FileList, KeyboardContext.CommandEntry })
        {
            Assert.AreSame(PaneSide.Left, PaneFilterPresenter.FocusTarget(closed, fixture.Session.Current, context));
        }
        Assert.IsNull(PaneFilterPresenter.FocusTarget(closed, fixture.Session.Current, KeyboardContext.TextEntry));
        _ = await fixture.HandleAsync(UserIntent.OpenSettings);
        Assert.IsNull(PaneFilterPresenter.FocusTarget(closed, fixture.Session.Current, KeyboardContext.PaneFilter));
    }

    /// <summary>Proves stale completion cannot steal focus from a replacement editor or a later same-side exit.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public async Task FocusTargetWhenEditorIsReplacedRejectsTheOldEffectAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PaneFilterClosed old = await CommitAsync(fixture);
        _ = await fixture.HandleAsync(UserIntent.OpenPaneFilter);
        PaneFilterEditing replacement = Assert.IsInstanceOfType<PaneFilterEditing>(fixture.Session.Current.Scopes.NameFilter);

        Assert.IsNull(PaneFilterPresenter.FocusTarget(old, fixture.Session.Current, KeyboardContext.PaneFilter));
        _ = await fixture.HandleAsync(UserIntent.FinishPaneFilter(replacement, PaneFilterEditAction.Commit, replacement.Query.Text));
        Assert.IsNull(PaneFilterPresenter.FocusTarget(old, fixture.Session.Current, KeyboardContext.PaneFilter));
        PaneFilterClosed current = Assert.IsInstanceOfType<PaneFilterClosed>(fixture.Session.Current.Scopes.NameFilter);
        Assert.AreSame(PaneSide.Left, PaneFilterPresenter.FocusTarget(current, fixture.Session.Current, KeyboardContext.PaneFilter));
    }

    /// <summary>Proves native blur and an accepted pane change cannot request focus back into the old pane.</summary>
    [TestMethod]
    public async Task FocusTargetWhenBlurOrPaneChangeOccursDoesNotReturnFocusAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PaneFilterClosed committed = await CommitAsync(fixture);
        _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);
        Assert.IsNull(PaneFilterPresenter.FocusTarget(committed, fixture.Session.Current, KeyboardContext.FileList));
        _ = await fixture.HandleAsync(UserIntent.OpenPaneFilter);
        PaneFilterEditing editing = Assert.IsInstanceOfType<PaneFilterEditing>(fixture.Session.Current.Scopes.NameFilter);
        _ = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Blur, editing.Query.Text));
        PaneFilterClosed blurred = Assert.IsInstanceOfType<PaneFilterClosed>(fixture.Session.Current.Scopes.NameFilter);

        Assert.IsNull(PaneFilterPresenter.FocusTarget(blurred, fixture.Session.Current, KeyboardContext.PaneFilter));
    }

    private static async Task<PaneFilterClosed> CommitAsync(PanePointerTestContext fixture)
    {
        _ = await fixture.HandleAsync(UserIntent.OpenPaneFilter);
        PaneFilterEditing editing = Assert.IsInstanceOfType<PaneFilterEditing>(fixture.Session.Current.Scopes.NameFilter);
        _ = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(editing, "a"));
        editing = Assert.IsInstanceOfType<PaneFilterEditing>(fixture.Session.Current.Scopes.NameFilter);
        _ = await fixture.HandleAsync(UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Commit, editing.Query.Text));
        return Assert.IsInstanceOfType<PaneFilterClosed>(fixture.Session.Current.Scopes.NameFilter);
    }
}
