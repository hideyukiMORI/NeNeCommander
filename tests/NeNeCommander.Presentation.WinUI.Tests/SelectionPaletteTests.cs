using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Commands;
using NeNeCommander.Presentation.WinUI.Help;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves palette and F1 selection entries project the canonical catalog and key map.</summary>
[TestClass]
public sealed class SelectionPaletteTests
{
    /// <summary>Proves all, inversion and clear are searchable with the correct canonical shortcuts.</summary>
    [TestMethod]
    public async Task PresentWhenSelectionCommandsAreSearchedUsesCanonicalLabelsAndShortcutsAsync()
    {
        CommandPaletteViewState view = CommandPalettePresenter.Present(
            await CommandPaletteTestContext.OpenPaletteAsync(null), CommandPaletteTestContext.Localize);
        UserIntent[] intents = [UserIntent.SelectAll, UserIntent.InvertSelection, UserIntent.ClearSelection];
        string[] labels = ["IntentLabelSelectAll", "IntentLabelInvertSelection", "IntentLabelClearSelection"];
        string[] caps = ["KeyLabelCtrlA", "KeyLabelStar", string.Empty];

        for (int index = 0; index < intents.Length; index++)
        {
            view.UpdateQuery(labels[index]);
            CommandPaletteRow row = view.Rows.Single();
            Assert.AreSame(intents[index], row.Intent);
            Assert.AreEqual(labels[index], row.Title);
            Assert.AreEqual(caps[index], row.Shortcut);
            Assert.IsTrue(row.IsAvailable);
            StringAssert.Contains(row.AutomationName, "Target: left pane");
        }
    }

    /// <summary>Proves every new binding appears once in F1 with its same label and key cap.</summary>
    [TestMethod]
    public void PresentWhenHelpListsSelectionReadsDeclaredBindings()
    {
        KeyboardHelpGroup group = KeyboardHelpPresenter.Present().PrimaryGroups.Single();
        UserIntent[] intents = [UserIntent.SelectAll, UserIntent.InvertSelection, UserIntent.SelectRangeNext,
            UserIntent.SelectRangePrevious, UserIntent.SelectRangeHalfPageDown, UserIntent.SelectRangeHalfPageUp,
            UserIntent.SelectRangeFirst, UserIntent.SelectRangeLast];

        foreach (UserIntent intent in intents)
        {
            string label = CommandLabelCatalog.LabelFor(intent).ResourceKey;
            KeyboardHelpRow row = group.Rows.Single(item => item.ActionLabelResourceKey == label);
            string cap = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList).Single(binding => binding.Intent == intent).KeyLabelResourceKey;
            CollectionAssert.AreEqual(new[] { cap }, row.KeyLabelResourceKeys.ToArray());
        }
        Assert.IsFalse(group.Rows.Any(row => row.ActionLabelResourceKey == "IntentLabelClearSelection"));
    }
}
