using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Windowing;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>
/// Proves the window-adjustment key map: the <c>Ctrl+W</c> entry on both translation routes, the
/// context-aware chord rule, the mode's dedicated table, its repeat rule, and what the mode
/// consumes or passes through.
/// </summary>
[TestClass]
public sealed class WindowAdjustmentKeyboardTests
{
    /// <summary>Proves <c>Ctrl+W</c> opens the mode from the file list and the navigation surface.</summary>
    [TestMethod]
    public void MapWhenControlWArrivesInANavigationContextOpensTheMode()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        Assert.AreSame(
            UserIntent.OpenWindowAdjustment,
            MappedIntent(mapper.Map(Input(KeyboardKey.W, KeyboardModifier.Control, KeyboardContext.FileList))));
        Assert.AreSame(
            UserIntent.OpenWindowAdjustment,
            MappedIntent(mapper.Map(Input(KeyboardKey.W, KeyboardModifier.Control, KeyboardContext.NavigationSurface))));
        Assert.AreEqual(
            "KeyLabelCtrlW",
            KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList)
                .Single(binding => binding.Intent == UserIntent.OpenWindowAdjustment).KeyLabelResourceKey);
    }

    /// <summary>Proves no other context maps <c>Ctrl+W</c> to the open intent, and a plain or Alt <c>w</c> opens nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public void MapWhenControlWArrivesElsewhereDoesNotOpenTheMode()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        foreach (KeyboardContext context in new[]
        {
            KeyboardContext.TextEntry,
            KeyboardContext.Modal,
            KeyboardContext.AddressEntry,
            KeyboardContext.CommandPalette,
        })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
                mapper.Map(Input(KeyboardKey.W, KeyboardModifier.Control, context)));
        }
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(Input(KeyboardKey.W, KeyboardModifier.None, KeyboardContext.FileList)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(Input(KeyboardKey.W, KeyboardModifier.Alt, KeyboardContext.NavigationSurface)));
        Assert.AreSame(
            WindowAdjustmentKeyAction.Leave,
            Assert.IsInstanceOfType<MappedWindowAdjustmentAction>(mapper.Map(Input(
                KeyboardKey.W,
                KeyboardModifier.Control,
                KeyboardContext.WindowAdjustment))).Action);
        foreach (KeyboardContext context in new[]
        {
            KeyboardContext.FileList,
            KeyboardContext.NavigationSurface,
            KeyboardContext.Modal,
            KeyboardContext.TextEntry,
            KeyboardContext.AddressEntry,
            KeyboardContext.WindowAdjustment,
        })
        {
            Assert.HasCount(
                context == KeyboardContext.FileList || context == KeyboardContext.NavigationSurface ? 1 : 0,
                KeyboardIntentMapper.BindingsFor(context)
                    .Where(binding => binding.Intent == UserIntent.OpenWindowAdjustment));
        }
    }

    /// <summary>Proves the virtual-key route translates only a Control-modified W.</summary>
    [TestMethod]
    public void TranslateKeyDataWhenWArrivesTranslatesOnlyControlW()
    {
        KeyboardInput control = KeyboardInputTranslator.TranslateKeyData(
            (int)VirtualKey.W,
            KeyRepeatState.Initial,
            KeyboardContext.NavigationSurface,
            KeyboardModifier.Control);
        KeyboardInput plain = KeyboardInputTranslator.TranslateKeyData(
            (int)VirtualKey.W,
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.None);
        KeyboardInput alt = KeyboardInputTranslator.TranslateKeyData(
            (int)VirtualKey.W,
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.Alt);

        Assert.AreSame(KeyboardKey.W, control.Key);
        Assert.AreSame(UserIntent.OpenWindowAdjustment, MappedIntent(CreateMapper().Map(control)));
        Assert.AreSame(KeyboardKey.Other, plain.Key);
        Assert.AreSame(KeyboardKey.Other, alt.Key);
    }

    /// <summary>Proves the character route produces W from both <c>w</c> and the control character U+0017.</summary>
    [TestMethod]
    public void TranslateCharacterDataWhenControlWArrivesTranslatesBothForms()
    {
        char[] characters = ['w', '\u0017'];

        foreach (KeyboardInput input in characters.Select(static character =>
            KeyboardInputTranslator.TranslateCharacterData(
                character,
                KeyRepeatState.Initial,
                KeyboardContext.FileList,
                KeyboardModifier.Control)))
        {
            Assert.AreSame(KeyboardKey.W, input.Key);
            Assert.AreSame(UserIntent.OpenWindowAdjustment, MappedIntent(CreateMapper().Map(input)));
        }
    }

    /// <summary>Proves the mode's produced characters have exact key identities.</summary>
    [TestMethod]
    public void TranslateCharacterDataWhenModeCharacterArrivesReturnsItsKey()
    {
        AssertCharacter('m', KeyboardKey.M);
        AssertCharacter('w', KeyboardKey.W);
        AssertCharacter('+', KeyboardKey.Plus);
        AssertCharacter('-', KeyboardKey.Minus);
        AssertCharacter('=', KeyboardKey.Other);
        AssertCharacter('M', KeyboardKey.Other);
        AssertCharacter('W', KeyboardKey.Other);
    }

    /// <summary>Proves the new keys name their own key-cap label resources.</summary>
    [TestMethod]
    public void LabelResourceKeyWhenModeKeyIsReadNamesItsResource()
    {
        Assert.AreEqual("KeyLabelM", KeyboardKey.M.LabelResourceKey);
        Assert.AreEqual("KeyLabelW", KeyboardKey.W.LabelResourceKey);
        Assert.AreEqual("KeyLabelPlus", KeyboardKey.Plus.LabelResourceKey);
        Assert.AreEqual("KeyLabelMinus", KeyboardKey.Minus.LabelResourceKey);
    }

    /// <summary>Proves keys the file list does not declare leave a pending <c>g</c> chord intact.</summary>
    [TestMethod]
    public void MapWhenModeKeyFollowsGInTheFileListKeepsTheChord()
    {
        foreach (KeyboardKey key in new[] { KeyboardKey.M, KeyboardKey.Plus, KeyboardKey.Minus })
        {
            KeyboardIntentMapper mapper = CreateMapper();
            _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));

            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Input(key)));

            Assert.AreSame(UserIntent.FocusFirst, MappedIntent(mapper.Map(Input(KeyboardKey.LowerG))));
        }
    }

    /// <summary>Proves <c>Ctrl+W</c> after a pending <c>g</c> cancels the chord and opens the mode.</summary>
    [TestMethod]
    public void MapWhenControlWFollowsGCancelsTheChordAndOpensTheMode()
    {
        KeyboardIntentMapper mapper = CreateMapper();
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));

        KeyboardMappingOutcome opened = mapper.Map(Input(KeyboardKey.W, KeyboardModifier.Control, KeyboardContext.FileList));

        Assert.AreSame(UserIntent.OpenWindowAdjustment, MappedIntent(opened));
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));
    }

    /// <summary>
    /// Proves the chord rule asks the current context: a key the file list itself declares cancels
    /// a pending <c>g</c>, so a following <c>g</c> starts a new chord instead of completing it.
    /// </summary>
    [TestMethod]
    public void MapWhenFileListDeclaredKeyFollowsGCancelsTheChord()
    {
        KeyboardIntentMapper mapper = CreateMapper();
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));

        Assert.AreSame(UserIntent.MoveNext, MappedIntent(mapper.Map(Input(KeyboardKey.J))));

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));
    }

    /// <summary>Proves a key the mode owns abandons a pending file-list chord.</summary>
    [TestMethod]
    public void MapWhenModeOwnsAKeyAfterGCancelsTheChord()
    {
        KeyboardIntentMapper mapper = CreateMapper();
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(Input(KeyboardKey.Other, KeyboardModifier.None, KeyboardContext.WindowAdjustment)));

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));
    }

    /// <summary>Proves every declared entry maps to its own action on an initial press.</summary>
    [TestMethod]
    public void MapWhenDeclaredKeyIsPressedInTheModeEmitsItsAction()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        foreach (WindowAdjustmentKeyBinding binding in KeyboardIntentMapper.WindowAdjustmentBindings)
        {
            MappedWindowAdjustmentAction mapped = Assert.IsInstanceOfType<MappedWindowAdjustmentAction>(
                mapper.Map(Input(binding.Key, binding.Modifier, KeyboardContext.WindowAdjustment)));
            Assert.AreSame(binding.Action, mapped.Action);
        }
    }

    /// <summary>Proves movement and help paging repeat while maximize, restore, transfer and leave do not.</summary>
    [TestMethod]
    public void MapWhenModeKeyRepeatsAcceptsMovementAndHelpPaging()
    {
        KeyboardIntentMapper mapper = CreateMapper();
        WindowAdjustmentKeyAction[] repeatable =
        [
            WindowAdjustmentKeyAction.MoveLeft,
            WindowAdjustmentKeyAction.MoveDown,
            WindowAdjustmentKeyAction.MoveUp,
            WindowAdjustmentKeyAction.MoveRight,
            WindowAdjustmentKeyAction.Enlarge,
            WindowAdjustmentKeyAction.Shrink,
            WindowAdjustmentKeyAction.PreviousHelpPage,
            WindowAdjustmentKeyAction.NextHelpPage,
        ];

        foreach (WindowAdjustmentKeyBinding binding in KeyboardIntentMapper.WindowAdjustmentBindings)
        {
            KeyboardMappingOutcome repeated = mapper.Map(KeyboardInput.Create(
                binding.Key,
                binding.Modifier,
                KeyRepeatState.Repeated,
                KeyboardContext.WindowAdjustment));
            if (repeatable.Contains(binding.Action))
            {
                Assert.AreSame(binding.Action, Assert.IsInstanceOfType<MappedWindowAdjustmentAction>(repeated).Action);
            }
            else
            {
                _ = Assert.IsInstanceOfType<KeyboardConsumed>(repeated);
            }
        }
    }

    /// <summary>
    /// Proves the raw virtual-key event the mode's produced characters arrive as passes through
    /// under every modifier and repeat state, so the following character still reaches the mode.
    /// </summary>
    [TestMethod]
    public void MapWhenUnidentifiedKeyArrivesInTheModePassesThrough()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        foreach (KeyboardModifier modifier in AllModifiers())
        {
            foreach (KeyRepeatState repeat in new[] { KeyRepeatState.Initial, KeyRepeatState.Repeated })
            {
                _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardInput.Create(
                    KeyboardKey.Other,
                    modifier,
                    repeat,
                    KeyboardContext.WindowAdjustment)));
            }
        }
    }

    /// <summary>
    /// Proves every identified key the mode does not declare, under every modifier, and every
    /// declared key under an undeclared modifier is consumed, so nothing reaches a pane, an editor,
    /// or a native control while the mode owns input.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public void MapWhenUndeclaredKeystrokeArrivesInTheModeConsumesIt()
    {
        KeyboardIntentMapper mapper = CreateMapper();
        IReadOnlyList<KeyboardKey> keys = AllKeys();
        int consumed = 0;

        foreach (KeyboardKey key in keys.Where(key => key != KeyboardKey.Other))
        {
            foreach (KeyboardModifier modifier in AllModifiers())
            {
                bool declared = KeyboardIntentMapper.WindowAdjustmentBindings.Any(binding =>
                    binding.Key == key && binding.Modifier == modifier);
                if (declared)
                {
                    continue;
                }
                _ = Assert.IsInstanceOfType<KeyboardConsumed>(
                    mapper.Map(Input(key, modifier, KeyboardContext.WindowAdjustment)),
                    key.LabelResourceKey + " " + modifier.GetType().Name + " must be consumed.");
                consumed++;
            }
        }

        Assert.HasCount(51, keys);
        Assert.AreEqual((50 * 5) - 20, consumed);
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(Input(KeyboardKey.F8, KeyboardModifier.None, KeyboardContext.WindowAdjustment)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(Input(KeyboardKey.Enter, KeyboardModifier.None, KeyboardContext.WindowAdjustment)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(Input(KeyboardKey.Tab, KeyboardModifier.None, KeyboardContext.WindowAdjustment)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(Input(KeyboardKey.H, KeyboardModifier.Control, KeyboardContext.WindowAdjustment)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(Input(KeyboardKey.W, KeyboardModifier.None, KeyboardContext.WindowAdjustment)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(Input(KeyboardKey.Escape, KeyboardModifier.Alt, KeyboardContext.WindowAdjustment)));
    }

    /// <summary>Proves an entry's key-cap label is the plain key label or the declared Control label.</summary>
    [TestMethod]
    public void KeyLabelResourceKeyWhenEntryIsReadNamesItsCap()
    {
        WindowAdjustmentKeyBinding[] bindings = [.. KeyboardIntentMapper.WindowAdjustmentBindings];
        WindowAdjustmentKeyBinding controlH = new(
            KeyboardKey.H,
            KeyboardModifier.Control,
            WindowAdjustmentKeyAction.MoveLeft,
            null);
        WindowAdjustmentKeyBinding altW = new(
            KeyboardKey.W,
            KeyboardModifier.Alt,
            WindowAdjustmentKeyAction.Leave,
            null);

        Assert.AreEqual("KeyLabelH", bindings[0].KeyLabelResourceKey);
        Assert.AreEqual("KeyLabelLeft", bindings[1].KeyLabelResourceKey);
        Assert.AreEqual("KeyLabelPlus", bindings[12].KeyLabelResourceKey);
        Assert.AreEqual("KeyLabelEscape", bindings[16].KeyLabelResourceKey);
        Assert.AreEqual("KeyLabelCtrlW", bindings[17].KeyLabelResourceKey);
        Assert.AreEqual("KeyLabelUnmapped", controlH.KeyLabelResourceKey);
        Assert.AreEqual("KeyLabelUnmapped", altW.KeyLabelResourceKey);
    }

    /// <summary>
    /// Proves each key action requests exactly its own Application window action, that leaving
    /// requests none, and that the correspondence covers every key action.
    /// </summary>
    [TestMethod]
    public void WindowActionWhenKeyActionIsReadNamesItsApplicationAction()
    {
        (WindowAdjustmentKeyAction Key, WindowAdjustmentAction? Window)[] expected =
        [
            (WindowAdjustmentKeyAction.MoveLeft, WindowAdjustmentAction.MoveLeft),
            (WindowAdjustmentKeyAction.MoveDown, WindowAdjustmentAction.MoveDown),
            (WindowAdjustmentKeyAction.MoveUp, WindowAdjustmentAction.MoveUp),
            (WindowAdjustmentKeyAction.MoveRight, WindowAdjustmentAction.MoveRight),
            (WindowAdjustmentKeyAction.Enlarge, WindowAdjustmentAction.Enlarge),
            (WindowAdjustmentKeyAction.Shrink, WindowAdjustmentAction.Shrink),
            (WindowAdjustmentKeyAction.Maximize, WindowAdjustmentAction.Maximize),
            (WindowAdjustmentKeyAction.Restore, WindowAdjustmentAction.Restore),
            (WindowAdjustmentKeyAction.PreviousDisplay, WindowAdjustmentAction.PreviousDisplay),
            (WindowAdjustmentKeyAction.NextDisplay, WindowAdjustmentAction.NextDisplay),
            (WindowAdjustmentKeyAction.Leave, null),
            (WindowAdjustmentKeyAction.PreviousHelpPage, null),
            (WindowAdjustmentKeyAction.NextHelpPage, null),
        ];

        foreach ((WindowAdjustmentKeyAction key, WindowAdjustmentAction? window) in expected)
        {
            Assert.AreSame(window, key.WindowAction);
        }
        CollectionAssert.AreEquivalent(AllKeyActions(), expected.Select(pair => pair.Key).ToArray());
        Assert.HasCount(
            10,
            AllKeyActions().Select(action => action.WindowAction).OfType<WindowAdjustmentAction>().Distinct());
    }

    /// <summary>Proves the table values reject every absent part.</summary>
    [TestMethod]
    public void ConstructWindowAdjustmentKeyValueWhenAPartIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowAdjustmentKeyBinding(
            null!,
            KeyboardModifier.None,
            WindowAdjustmentKeyAction.Leave,
            null));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowAdjustmentKeyBinding(
            KeyboardKey.Escape,
            null!,
            WindowAdjustmentKeyAction.Leave,
            null));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowAdjustmentKeyBinding(
            KeyboardKey.Escape,
            KeyboardModifier.None,
            null!,
            null));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new MappedWindowAdjustmentAction(null!));
    }

    internal static IReadOnlyList<KeyboardKey> AllKeys()
    {
        return [.. typeof(KeyboardKey)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(KeyboardKey))
            .Select(property => (KeyboardKey)property.GetValue(null)!)];
    }

    private static KeyboardModifier[] AllModifiers()
    {
        return [.. typeof(KeyboardModifier)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(KeyboardModifier))
            .Select(property => (KeyboardModifier)property.GetValue(null)!)];
    }

    internal static WindowAdjustmentKeyAction[] AllKeyActions()
    {
        return [.. typeof(WindowAdjustmentKeyAction)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(property => (WindowAdjustmentKeyAction)property.GetValue(null)!)];
    }

    private static void AssertCharacter(char character, KeyboardKey expected)
    {
        KeyboardInput input = KeyboardInputTranslator.TranslateCharacterData(
            character,
            KeyRepeatState.Initial,
            KeyboardContext.WindowAdjustment,
            KeyboardModifier.None);
        Assert.AreSame(expected, input.Key);
    }

    private static UserIntent MappedIntent(KeyboardMappingOutcome outcome)
    {
        return Assert.IsInstanceOfType<MappedKeyboardIntent>(outcome).Intent;
    }

    private static KeyboardIntentMapper CreateMapper()
    {
        return new KeyboardIntentMapper(AdjustableClock.Create());
    }

    private static KeyboardInput Input(KeyboardKey key)
    {
        return Input(key, KeyboardModifier.None, KeyboardContext.FileList);
    }

    private static KeyboardInput Input(KeyboardKey key, KeyboardModifier modifier, KeyboardContext context)
    {
        return KeyboardInput.Create(key, modifier, KeyRepeatState.Initial, context);
    }
}
