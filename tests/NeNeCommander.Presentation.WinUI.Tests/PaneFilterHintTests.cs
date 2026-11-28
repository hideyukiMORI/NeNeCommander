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
    [DataRow("en-US", "All words must occur in the name. Enter: keep filter. Esc: clear filter.")]
    [DataRow("ja-JP", "すべての語を名前に含む項目を表示します。Enter: 条件を保持。Esc: 条件を解除。")]
    [DataRow("zh-Hans", "名称必须包含所有词语。Enter：保留筛选。Esc：清除筛选。")]
    public async Task ComposeHintWhenEditingUsesLocalizedCanonicalKeyCapsAsync(string locale, string expected)
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        _ = await fixture.HandleAsync(UserIntent.OpenPaneFilter);
        PaneFilterPresentation presentation = PaneFilterPresenter.Present(fixture.Session.Current, PaneSide.Left, null);
        PresentationResourceCatalog catalog = new(locale);
        List<string> requested = [];
        string Localize(string key)
        {
            requested.Add(key);
            return catalog.Get(key);
        }

        string hint = PaneFilterPresenter.ComposeHint(presentation, Localize);

        Assert.AreEqual(expected, hint);
        string[] expectedKeys = ["PaneFilterEditingHint", "KeyLabelEnter", "KeyLabelEscape"];
        CollectionAssert.AreEqual(expectedKeys, requested);
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

        Assert.AreEqual("Name filter active. Click the field to edit it.", PaneFilterPresenter.ComposeHint(applied, PresentationResourceCatalog.English.Get));
        Assert.AreEqual("No visible items match this name filter.", PaneFilterPresenter.ComposeHint(unmatched, PresentationResourceCatalog.English.Get));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneFilterPresenter.ComposeHint(null!, _ => throw new AssertFailedException("No localizer may run for an absent presentation.")));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneFilterPresenter.ComposeHint(applied, null!));
    }
}
