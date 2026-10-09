using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the closed sort indication and the localized pane status line.</summary>
[TestClass]
public sealed class PaneStatusLineTests
{
    /// <summary>Proves every key and direction names its own indication and resource.</summary>
    [TestMethod]
    public void ForWhenOrderVariesNamesExactIndication()
    {
        PaneSortOrder nameDescending = PaneSortOrder.Default.Toggle(SortKey.Name);
        PaneSortOrder extensionAscending = PaneSortOrder.Default.Toggle(SortKey.Extension);
        PaneSortOrder extensionDescending = extensionAscending.Toggle(SortKey.Extension);

        Assert.AreSame(PaneSortStatus.NameAscending, PaneSortStatus.For(PaneSortOrder.Default));
        Assert.AreSame(PaneSortStatus.NameDescending, PaneSortStatus.For(nameDescending));
        Assert.AreSame(PaneSortStatus.ExtensionAscending, PaneSortStatus.For(extensionAscending));
        Assert.AreSame(PaneSortStatus.ExtensionDescending, PaneSortStatus.For(extensionDescending));
        Assert.AreEqual("PaneSortNameAscending", PaneSortStatus.NameAscending.ResourceKey);
        Assert.AreEqual("PaneSortNameDescending", PaneSortStatus.NameDescending.ResourceKey);
        Assert.AreEqual("PaneSortExtensionAscending", PaneSortStatus.ExtensionAscending.ResourceKey);
        Assert.AreEqual("PaneSortExtensionDescending", PaneSortStatus.ExtensionDescending.ResourceKey);
    }

    /// <summary>Proves a listed pane shows its status and sort indication through the line format.</summary>
    [TestMethod]
    public void ComposeWhenSortStatusIsPresentFormatsStatusAndIndication()
    {
        string line = PaneStatusLine.Compose(PaneStatus.Complete, PaneSortStatus.ExtensionDescending, Localize);

        Assert.AreEqual("Listing complete | ext desc", line);
    }

    /// <summary>Proves a pane without a listing shows its status alone without the line format.</summary>
    [TestMethod]
    public void ComposeWhenSortStatusIsAbsentShowsStatusAlone()
    {
        string line = PaneStatusLine.Compose(PaneStatus.NoListing, null, Localize);

        Assert.AreEqual("Nothing listed", line);
    }

    /// <summary>Proves the status, the localizer, and the order are required.</summary>
    [TestMethod]
    public void ComposeWhenArgumentIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            PaneStatusLine.Compose(null!, PaneSortStatus.NameAscending, Localize));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            PaneStatusLine.Compose(PaneStatus.Complete, PaneSortStatus.NameAscending, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneSortStatus.For(null!));
    }

    private static string Localize(string key)
    {
        Dictionary<string, string> values = new()
        {
            ["PaneStatusComplete"] = "Listing complete",
            ["PaneStatusNoListing"] = "Nothing listed",
            ["PaneStatusSortFormat"] = "{0} | {1}",
            ["PaneSortExtensionDescending"] = "ext desc",
        };
        return values[key];
    }
}
