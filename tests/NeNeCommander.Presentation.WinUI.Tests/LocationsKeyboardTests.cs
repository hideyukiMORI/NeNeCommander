using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;
using Windows.System;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the Ctrl+G context matrix and the Locations picker's key ownership (KBD-002).</summary>
[TestClass]
public sealed class LocationsKeyboardTests
{
    /// <summary>Proves Ctrl+G opens the picker from the file list and the navigation surface only.</summary>
    [TestMethod]
    public void MapWhenControlGArrivesOpensLocationsOnlyFromFileListAndNavigationSurface()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        AssertMaps(mapper, Input(KeyboardKey.LowerG, KeyboardModifier.Control, KeyboardContext.FileList), UserIntent.OpenLocations);
        AssertMaps(mapper, Input(KeyboardKey.LowerG, KeyboardModifier.Control, KeyboardContext.NavigationSurface), UserIntent.OpenLocations);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(Input(KeyboardKey.LowerG, KeyboardModifier.Control, KeyboardContext.TextEntry)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(Input(KeyboardKey.LowerG, KeyboardModifier.Control, KeyboardContext.AddressEntry)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(Input(KeyboardKey.LowerG, KeyboardModifier.Control, KeyboardContext.Modal)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(Input(KeyboardKey.LowerG, KeyboardModifier.Control, KeyboardContext.CommandPalette)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(Input(KeyboardKey.LowerG, KeyboardModifier.Control, KeyboardContext.WindowAdjustment)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(Input(KeyboardKey.LowerG, KeyboardModifier.Control, KeyboardContext.Locations)));
    }

    /// <summary>Proves plain g still starts the movement chord and Ctrl+G cancels a pending one.</summary>
    [TestMethod]
    public void MapWhenChordIsPendingControlGOpensLocationsAndCancelsTheChord()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(
            mapper.Map(Input(KeyboardKey.LowerG, KeyboardModifier.None, KeyboardContext.FileList)));
        AssertMaps(mapper, Input(KeyboardKey.LowerG, KeyboardModifier.Control, KeyboardContext.FileList), UserIntent.OpenLocations);

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(
            mapper.Map(Input(KeyboardKey.LowerG, KeyboardModifier.None, KeyboardContext.FileList)));
    }

    /// <summary>Proves the picker's declared keys map to focus movement, selection, and closing.</summary>
    [TestMethod]
    public void MapWhenPickerOwnsInputMapsDeclaredKeys()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        AssertMaps(mapper, Picker(KeyboardKey.J), UserIntent.MoveNext);
        AssertMaps(mapper, Picker(KeyboardKey.Down), UserIntent.MoveNext);
        AssertMaps(mapper, Picker(KeyboardKey.K), UserIntent.MovePrevious);
        AssertMaps(mapper, Picker(KeyboardKey.Up), UserIntent.MovePrevious);
        AssertMaps(mapper, Picker(KeyboardKey.Enter), UserIntent.Confirm);
        AssertMaps(mapper, Picker(KeyboardKey.Escape), UserIntent.Escape);
        AssertMaps(
            mapper,
            KeyboardInput.Create(KeyboardKey.J, KeyboardModifier.None, KeyRepeatState.Repeated, KeyboardContext.Locations),
            UserIntent.MoveNext);
    }

    /// <summary>Proves every other identified key, a held Enter, and a modified declared key are consumed.</summary>
    [TestMethod]
    public void MapWhenPickerOwnsInputConsumesEveryUndeclaredKey()
    {
        KeyboardIntentMapper mapper = CreateMapper();
        KeyboardInput[] consumed =
        [
            Picker(KeyboardKey.Tab),
            Picker(KeyboardKey.Space),
            Picker(KeyboardKey.H),
            Picker(KeyboardKey.L),
            Picker(KeyboardKey.UpperG),
            Picker(KeyboardKey.LowerG),
            Picker(KeyboardKey.F5),
            Picker(KeyboardKey.F8),
            Picker(KeyboardKey.PageDown),
            Input(KeyboardKey.B, KeyboardModifier.Control, KeyboardContext.Locations),
            Input(KeyboardKey.J, KeyboardModifier.Control, KeyboardContext.Locations),
            Input(KeyboardKey.Escape, KeyboardModifier.Alt, KeyboardContext.Locations),
            Input(KeyboardKey.Enter, KeyboardModifier.Other, KeyboardContext.Locations),
            KeyboardInput.Create(KeyboardKey.Enter, KeyboardModifier.None, KeyRepeatState.Repeated, KeyboardContext.Locations),
        ];

        foreach (KeyboardInput input in consumed)
        {
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(input));
        }
    }

    /// <summary>Proves the raw event before a produced character passes through so j and k still arrive.</summary>
    [TestMethod]
    public void MapWhenPickerReceivesRawVirtualKeyPassesItThrough()
    {
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(CreateMapper().Map(Picker(KeyboardKey.Other)));
    }

    /// <summary>Proves entering the picker discards a pending chord.</summary>
    [TestMethod]
    public void MapWhenPickerReceivesKeyClearsPendingChord()
    {
        KeyboardIntentMapper mapper = CreateMapper();
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(
            mapper.Map(Input(KeyboardKey.LowerG, KeyboardModifier.None, KeyboardContext.FileList)));

        _ = mapper.Map(Picker(KeyboardKey.Down));

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(
            mapper.Map(Input(KeyboardKey.LowerG, KeyboardModifier.None, KeyboardContext.FileList)));
    }

    /// <summary>Proves the raw G virtual key and the produced Ctrl+G character translate under Control.</summary>
    [TestMethod]
    public void TranslateWhenControlGArrivesProducesLowerGWithControl()
    {
        KeyboardInput raw = KeyboardInputTranslator.TranslateKeyData(
            (int)VirtualKey.G,
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.Control);
        KeyboardInput plainRaw = KeyboardInputTranslator.TranslateKeyData(
            (int)VirtualKey.G,
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.None);
        KeyboardInput produced = KeyboardInputTranslator.TranslateCharacterData(
            '\u0007',
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.Control);

        Assert.AreSame(KeyboardKey.LowerG, raw.Key);
        Assert.AreSame(KeyboardModifier.Control, raw.Modifier);
        Assert.AreSame(KeyboardKey.Other, plainRaw.Key);
        Assert.AreSame(KeyboardKey.LowerG, produced.Key);
        AssertMaps(CreateMapper(), raw, UserIntent.OpenLocations);
    }

    /// <summary>Proves the Ctrl+G binding names its chord cap and the picker hints come from its bindings.</summary>
    [TestMethod]
    public void HintsWhenPickerOwnsInputAreGeneratedFromItsBindings()
    {
        IReadOnlyList<KeyHint> hints = KeyHintPresenter.Present(KeyboardContext.Locations);
        KeyBinding open = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList)
            .Single(binding => binding.Intent == UserIntent.OpenLocations);

        Assert.AreEqual("KeyLabelCtrlG", open.KeyLabelResourceKey);
        Assert.HasCount(4, hints);
        AssertHint(hints[0], "KeyLabelJ", "IntentLabelMoveNext");
        AssertHint(hints[1], "KeyLabelK", "IntentLabelMovePrevious");
        AssertHint(hints[2], "KeyLabelEnter", "IntentLabelConfirm");
        AssertHint(hints[3], "KeyLabelEscape", "IntentLabelEscape");
    }

    private static void AssertHint(KeyHint hint, string keyLabel, string intentLabel)
    {
        Assert.AreEqual(keyLabel, hint.KeyLabelResourceKey);
        Assert.AreEqual(intentLabel, hint.IntentLabelResourceKey);
    }

    private static KeyboardIntentMapper CreateMapper()
    {
        return new KeyboardIntentMapper(AdjustableClock.Create());
    }

    private static KeyboardInput Picker(KeyboardKey key)
    {
        return Input(key, KeyboardModifier.None, KeyboardContext.Locations);
    }

    private static KeyboardInput Input(KeyboardKey key, KeyboardModifier modifier, KeyboardContext context)
    {
        return KeyboardInput.Create(key, modifier, KeyRepeatState.Initial, context);
    }

    private static void AssertMaps(KeyboardIntentMapper mapper, KeyboardInput input, UserIntent expected)
    {
        MappedKeyboardIntent mapped = Assert.IsInstanceOfType<MappedKeyboardIntent>(mapper.Map(input));
        Assert.AreSame(expected, mapped.Intent);
    }
}
