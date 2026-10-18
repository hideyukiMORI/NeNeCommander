using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Commands;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the CommandPaletteKeyHintPresenter contract.</summary>
[TestClass]
public sealed class CommandPaletteKeyHintPresenterTests
{
    /// <summary>Proves displayed palette hints project the canonical palette binding order.</summary>
    [TestMethod]
    public void KeyHintsComeFromCanonicalPaletteBindings()
    {
        IReadOnlyList<CommandPaletteKeyHint> hints = CommandPaletteKeyHintPresenter.Present();

        Assert.HasCount(5, hints);
        Assert.AreEqual("KeyLabelUp", hints[0].KeyLabelResourceKey);
        Assert.AreEqual("CommandPaletteHintPrevious", hints[0].IntentLabelResourceKey);
        Assert.AreEqual("KeyLabelDown", hints[1].KeyLabelResourceKey);
        Assert.AreEqual("CommandPaletteHintNext", hints[1].IntentLabelResourceKey);
        Assert.AreEqual("KeyLabelEnter", hints[2].KeyLabelResourceKey);
        Assert.AreEqual("CommandPaletteHintExecute", hints[2].IntentLabelResourceKey);
        Assert.AreEqual("KeyLabelEscape", hints[3].KeyLabelResourceKey);
        Assert.AreEqual("CommandPaletteHintCancel", hints[3].IntentLabelResourceKey);
        Assert.AreEqual("KeyLabelTab", hints[4].KeyLabelResourceKey);
        Assert.AreEqual("CommandPaletteHintFocus", hints[4].IntentLabelResourceKey);
        Assert.IsTrue(hints.All(hint => hint.AutomationId == string.Empty));
    }
}
