using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the complete adjustment declaration independently of runtime key mapping.</summary>
[TestClass]
public sealed class WindowAdjustmentBindingTests
{
    /// <summary>Proves the dedicated table declares exactly twenty entries for thirteen actions in declaration order.</summary>
    [TestMethod]
    public void WindowAdjustmentBindingsWhenReadDeclareTheExactTable()
    {
        (KeyboardKey Key, KeyboardModifier Modifier, WindowAdjustmentKeyAction Action, WindowAdjustmentHintGroup? Group)[] expected =
        [
            (KeyboardKey.H, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveLeft, WindowAdjustmentHintGroup.Move),
            (KeyboardKey.Left, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveLeft, null),
            (KeyboardKey.J, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveDown, WindowAdjustmentHintGroup.Move),
            (KeyboardKey.Down, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveDown, null),
            (KeyboardKey.K, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveUp, WindowAdjustmentHintGroup.Move),
            (KeyboardKey.Up, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveUp, null),
            (KeyboardKey.L, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveRight, WindowAdjustmentHintGroup.Move),
            (KeyboardKey.Right, KeyboardModifier.None, WindowAdjustmentKeyAction.MoveRight, null),
            (KeyboardKey.P, KeyboardModifier.None, WindowAdjustmentKeyAction.PreviousDisplay, WindowAdjustmentHintGroup.PreviousDisplay),
            (KeyboardKey.Left, KeyboardModifier.Control, WindowAdjustmentKeyAction.PreviousDisplay, WindowAdjustmentHintGroup.PreviousDisplay),
            (KeyboardKey.N, KeyboardModifier.None, WindowAdjustmentKeyAction.NextDisplay, WindowAdjustmentHintGroup.NextDisplay),
            (KeyboardKey.Right, KeyboardModifier.Control, WindowAdjustmentKeyAction.NextDisplay, WindowAdjustmentHintGroup.NextDisplay),
            (KeyboardKey.Plus, KeyboardModifier.None, WindowAdjustmentKeyAction.Enlarge, WindowAdjustmentHintGroup.Enlarge),
            (KeyboardKey.Minus, KeyboardModifier.None, WindowAdjustmentKeyAction.Shrink, WindowAdjustmentHintGroup.Shrink),
            (KeyboardKey.M, KeyboardModifier.None, WindowAdjustmentKeyAction.Maximize, WindowAdjustmentHintGroup.Maximize),
            (KeyboardKey.R, KeyboardModifier.None, WindowAdjustmentKeyAction.Restore, WindowAdjustmentHintGroup.Restore),
            (KeyboardKey.Escape, KeyboardModifier.None, WindowAdjustmentKeyAction.Leave, WindowAdjustmentHintGroup.Leave),
            (KeyboardKey.W, KeyboardModifier.Control, WindowAdjustmentKeyAction.Leave, null),
            (KeyboardKey.PageUp, KeyboardModifier.None, WindowAdjustmentKeyAction.PreviousHelpPage, WindowAdjustmentHintGroup.ScrollHelp),
            (KeyboardKey.PageDown, KeyboardModifier.None, WindowAdjustmentKeyAction.NextHelpPage, WindowAdjustmentHintGroup.ScrollHelp),
        ];
        IReadOnlyList<WindowAdjustmentKeyBinding> bindings = KeyboardIntentMapper.WindowAdjustmentBindings;

        Assert.HasCount(20, bindings);
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.AreSame(expected[index].Key, bindings[index].Key);
            Assert.AreSame(expected[index].Modifier, bindings[index].Modifier);
            Assert.AreSame(expected[index].Action, bindings[index].Action);
            Assert.AreSame(expected[index].Group, bindings[index].HintGroup);
        }
        Assert.HasCount(13, bindings.Select(binding => binding.Action).Distinct());
        Assert.HasCount(13, WindowAdjustmentKeyboardTests.AllKeyActions());
        Assert.HasCount(0, KeyboardIntentMapper.BindingsFor(KeyboardContext.WindowAdjustment));
    }

}
