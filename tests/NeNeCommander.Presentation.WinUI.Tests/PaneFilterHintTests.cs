using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves complete filter guidance and shortcut labels come from the resource owner.</summary>
[TestClass]
public sealed class PaneFilterHintTests
{
    /// <summary>Proves editor guidance resolves the mapper's two key identities through the supplied locale.</summary>
    [TestMethod]
    public async Task ComposeHintWhenEditingUsesLocalizedCanonicalKeyCapsAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenPaneFilter);
        PaneFilterPresentation presentation = PaneFilterPresenter.Present(fixture.Session.Current, PaneSide.Left, null);
        List<string> requested = [];
        string Localize(string key)
        {
            requested.Add(key);
            return key == "PaneFilterEditingHint" ? "commit [{0}], clear [{1}]" : "localized:" + key;
        }

        string hint = PaneFilterPresenter.ComposeHint(presentation, Localize);

        Assert.AreEqual("commit [localized:KeyLabelEnter], clear [localized:KeyLabelEscape]", hint);
        string[] expected = ["PaneFilterEditingHint", "KeyLabelEnter", "KeyLabelEscape"];
        CollectionAssert.AreEqual(expected, requested);
    }

    /// <summary>Proves applied and unmatched queries each retain a complete distinct resource sentence.</summary>
    [TestMethod]
    public async Task ComposeHintWhenAppliedOrUnmatchedResolvesTheCompleteResourceAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenPaneFilter);
        PaneFilterEditing owner = Assert.IsInstanceOfType<PaneFilterEditing>(fixture.Session.Current.Scopes.NameFilter);
        _ = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(owner, "a"));
        owner = Assert.IsInstanceOfType<PaneFilterEditing>(fixture.Session.Current.Scopes.NameFilter);
        _ = await fixture.HandleAsync(UserIntent.FinishPaneFilter(owner, PaneFilterEditAction.Blur, owner.Query.Text));
        PaneFilterPresentation applied = PaneFilterPresenter.Present(fixture.Session.Current, PaneSide.Left, null);
        _ = await fixture.HandleAsync(UserIntent.OpenPaneFilter);
        owner = Assert.IsInstanceOfType<PaneFilterEditing>(fixture.Session.Current.Scopes.NameFilter);
        _ = await fixture.HandleAsync(UserIntent.ChangePaneFilterText(owner, "absent"));
        PaneFilterPresentation unmatched = PaneFilterPresenter.Present(fixture.Session.Current, PaneSide.Left, applied);

        Assert.AreEqual("whole:PaneFilterAppliedHint", PaneFilterPresenter.ComposeHint(applied, key => "whole:" + key));
        Assert.AreEqual("whole:PaneFilterNoMatches", PaneFilterPresenter.ComposeHint(unmatched, key => "whole:" + key));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneFilterPresenter.ComposeHint(null!, key => key));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneFilterPresenter.ComposeHint(applied, null!));
    }
}
