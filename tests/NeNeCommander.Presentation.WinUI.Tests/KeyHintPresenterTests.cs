using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the displayed shortcut hints are generated from the canonical key map (KBD-005).</summary>
[TestClass]
public sealed class KeyHintPresenterTests
{
    /// <summary>Proves the file-list context shows the file commands in the approved order.</summary>
    [TestMethod]
    public void PresentWhenContextIsFileListShowsTheFileCommandsInOrder()
    {
        IReadOnlyList<KeyHint> hints = KeyHintPresenter.Present(KeyboardContext.FileList);

        Assert.HasCount(10, hints);
        AssertHint(hints[0], "KeyLabelF2", "IntentLabelRename");
        AssertHint(hints[1], "KeyLabelF5", "IntentLabelCopy");
        AssertHint(hints[2], "KeyLabelF6", "IntentLabelMove");
        AssertHint(hints[3], "KeyLabelF7", "IntentLabelCreateDirectory");
        AssertHint(hints[4], "KeyLabelF8", "IntentLabelDelete");
        AssertHint(hints[5], "KeyLabelTab", "IntentLabelActivateOtherPane");
        AssertHint(hints[6], "KeyLabelCtrlH", "IntentLabelToggleHiddenItems");
        AssertHint(hints[7], "KeyLabelCtrlB", "IntentLabelOpenBookmarks");
        AssertHint(hints[8], "KeyLabelCtrlComma", "IntentLabelOpenSettings");
        AssertHint(hints[9], "KeyLabelEscape", "IntentLabelEscape");
    }

    /// <summary>Proves a pending modal shows only its two declared keys.</summary>
    [TestMethod]
    public void PresentWhenContextIsModalShowsOnlyConfirmAndEscape()
    {
        IReadOnlyList<KeyHint> hints = KeyHintPresenter.Present(KeyboardContext.Modal);

        Assert.HasCount(2, hints);
        AssertHint(hints[0], "KeyLabelEnter", "IntentLabelConfirm");
        AssertHint(hints[1], "KeyLabelEscape", "IntentLabelEscape");
    }

    /// <summary>Proves a context with no declared projection shows nothing.</summary>
    [TestMethod]
    public void PresentWhenContextDeclaresNoProjectionShowsNoHints()
    {
        Assert.IsEmpty(KeyHintPresenter.Present(KeyboardContext.TextEntry));
        Assert.IsEmpty(KeyHintPresenter.Present(KeyboardContext.NavigationSurface));
    }

    /// <summary>Proves the presenter rejects an absent context.</summary>
    [TestMethod]
    public void PresentWhenContextIsNullThrowsArgumentNullException()
    {
        MethodInfo method = typeof(KeyHintPresenter).GetMethod(
            nameof(KeyHintPresenter.Present),
            BindingFlags.Public | BindingFlags.Static) ??
            throw new AssertFailedException("The present method was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    /// <summary>Proves every hint names a keystroke the canonical map really performs.</summary>
    [TestMethod]
    public void PresentWhenHintsAreShownEveryKeyLabelComesFromADeclaredBinding()
    {
        AssertLabelsComeFromBindings(KeyboardContext.FileList);
        AssertLabelsComeFromBindings(KeyboardContext.Modal);
    }

    /// <summary>Proves the binding-derived label projection preserves every declared modifier chord.</summary>
    [TestMethod]
    public void KeyLabelResourceKeyWhenBindingUsesModifierReturnsExactChordLabel()
    {
        AssertBindingLabel(KeyboardModifier.None, KeyboardKey.F5, "KeyLabelF5");
        AssertBindingLabel(KeyboardModifier.Control, KeyboardKey.D, "KeyLabelCtrlD");
        AssertBindingLabel(KeyboardModifier.Control, KeyboardKey.B, "KeyLabelCtrlB");
        AssertBindingLabel(KeyboardModifier.Control, KeyboardKey.H, "KeyLabelCtrlH");
        AssertBindingLabel(KeyboardModifier.Control, KeyboardKey.Comma, "KeyLabelCtrlComma");
        AssertBindingLabel(KeyboardModifier.Control, KeyboardKey.L, "KeyLabelCtrlL");
        AssertBindingLabel(KeyboardModifier.Control, KeyboardKey.R, "KeyLabelCtrlR");
        AssertBindingLabel(KeyboardModifier.Control, KeyboardKey.U, "KeyLabelCtrlU");
        AssertBindingLabel(KeyboardModifier.Control, KeyboardKey.One, "KeyLabelCtrl1");
        AssertBindingLabel(KeyboardModifier.Control, KeyboardKey.Nine, "KeyLabelCtrl9");
        AssertBindingLabel(KeyboardModifier.Alt, KeyboardKey.Up, "KeyLabelAltUp");
        AssertBindingLabel(KeyboardModifier.Control, KeyboardKey.Up, "KeyLabelUnmapped");
        AssertBindingLabel(KeyboardModifier.Alt, KeyboardKey.D, "KeyLabelUnmapped");
    }

    private static void AssertLabelsComeFromBindings(KeyboardContext context)
    {
        HashSet<string> declaredLabels = [];
        foreach (KeyBinding binding in KeyboardIntentMapper.BindingsFor(context))
        {
            _ = declaredLabels.Add(binding.KeyLabelResourceKey);
        }
        foreach (KeyHint hint in KeyHintPresenter.Present(context))
        {
            Assert.Contains(hint.KeyLabelResourceKey, declaredLabels);
        }
    }

    private static void AssertHint(KeyHint hint, string keyLabelResourceKey, string intentLabelResourceKey)
    {
        Assert.AreEqual(keyLabelResourceKey, hint.KeyLabelResourceKey);
        Assert.AreEqual(intentLabelResourceKey, hint.IntentLabelResourceKey);
    }

    private static void AssertBindingLabel(
        KeyboardModifier modifier,
        KeyboardKey key,
        string expectedResourceKey)
    {
        KeyBinding binding = new(KeyboardContext.FileList, key, modifier, UserIntent.Refresh);

        Assert.AreEqual(expectedResourceKey, binding.KeyLabelResourceKey);
    }
}
