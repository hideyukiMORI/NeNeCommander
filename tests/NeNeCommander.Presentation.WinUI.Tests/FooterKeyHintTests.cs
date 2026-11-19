using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves hiding normal footer hints retains help, required guidance and pane presentation.</summary>
[TestClass]
public sealed class FooterKeyHintTests
{
    /// <summary>Proves the off state retains the exact canonical help key and label.</summary>
    [TestMethod]
    public void PresentWhenFileListHintsAreHiddenKeepsOnlyCanonicalHelp()
    {
        IReadOnlyList<KeyHint> hints = KeyHintPresenter.Present(KeyboardContext.FileList, KeyHintVisibility.Hidden);
        KeyBinding help = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList)
            .Single(binding => binding.Intent == UserIntent.OpenHelp);

        Assert.HasCount(1, hints);
        Assert.AreEqual(help.KeyLabelResourceKey, hints[0].KeyLabelResourceKey);
        Assert.AreEqual("IntentLabelOpenHelp", hints[0].IntentLabelResourceKey);
    }

    /// <summary>Proves confirmation/name-entry and Locations hints are outside the preference's scope.</summary>
    [TestMethod]
    public void PresentWhenOtherContextsOwnInputKeepsAllTheirHints()
    {
        foreach (KeyboardContext context in new[] { KeyboardContext.Modal, KeyboardContext.Locations })
        {
            IReadOnlyList<KeyHint> shown = KeyHintPresenter.Present(context, KeyHintVisibility.Shown);
            IReadOnlyList<KeyHint> hidden = KeyHintPresenter.Present(context, KeyHintVisibility.Hidden);

            CollectionAssert.AreEqual(shown.ToArray(), hidden.ToArray());
            Assert.IsTrue(hidden.Any(hint => hint.IntentLabelResourceKey == "IntentLabelConfirm"));
            Assert.IsTrue(hidden.Any(hint => hint.IntentLabelResourceKey == "IntentLabelEscape"));
        }
    }

    /// <summary>Proves switching the preference updates hints without recreating the pane rows or operation state.</summary>
    [TestMethod]
    public void PresentWhenHintPreferenceChangesReusesRowsAndOperationInformation()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out _, out _, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        DualPanePresentation shown = DualPanePresenter.Present(panes.Current, TestMetadataFormats.Utc, KeyHintVisibility.Shown);

        DualPanePresentation hidden = DualPanePresenter.Present(panes.Current, TestMetadataFormats.Utc, shown, KeyHintVisibility.Hidden);
        DualPanePresentation restored = DualPanePresenter.Present(panes.Current, TestMetadataFormats.Utc, hidden, KeyHintVisibility.Shown);

        Assert.HasCount(1, hidden.KeyHints);
        Assert.AreEqual(shown.KeyHints.Count, restored.KeyHints.Count);
        Assert.AreSame(shown.Left.Rows, hidden.Left.Rows);
        Assert.AreSame(shown.Right.Rows, hidden.Right.Rows);
        Assert.AreSame(shown.OperationStatus, hidden.OperationStatus);
        Assert.AreSame(shown.Detail, hidden.Detail);
        Assert.AreSame(shown.InputContext, hidden.InputContext);
    }
}
