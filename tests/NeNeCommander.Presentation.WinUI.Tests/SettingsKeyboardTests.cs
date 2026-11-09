using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>
/// Proves the settings editor's key map and focus order (ADR-0077): <c>j</c>/<c>k</c> and the
/// arrows move, <c>Space</c>/<c>Enter</c> activate once, and undeclared keys stay native.
/// </summary>
[TestClass]
public sealed class SettingsKeyboardTests
{
    /// <summary>Proves the letters and the arrows move, and both activation keys confirm.</summary>
    [TestMethod]
    public void MapWhenSettingsOwnsInputMapsMovementActivationAndClosing()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        Assert.AreSame(UserIntent.MoveNext, Mapped(mapper, KeyboardKey.J));
        Assert.AreSame(UserIntent.MoveNext, Mapped(mapper, KeyboardKey.Down));
        Assert.AreSame(UserIntent.MovePrevious, Mapped(mapper, KeyboardKey.K));
        Assert.AreSame(UserIntent.MovePrevious, Mapped(mapper, KeyboardKey.Up));
        Assert.AreSame(UserIntent.Confirm, Mapped(mapper, KeyboardKey.Space));
        Assert.AreSame(UserIntent.Confirm, Mapped(mapper, KeyboardKey.Enter));
        Assert.AreSame(UserIntent.Escape, Mapped(mapper, KeyboardKey.Escape));
    }

    /// <summary>Proves a held activation key changes a setting once while held movement repeats.</summary>
    [TestMethod]
    public void MapWhenActivationRepeatsConsumesItAndMovementRepeats()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(Input(KeyboardKey.Space, KeyboardModifier.None, KeyRepeatState.Repeated)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(Input(KeyboardKey.Enter, KeyboardModifier.None, KeyRepeatState.Repeated)));
        Assert.AreSame(
            UserIntent.MoveNext,
            Assert.IsInstanceOfType<MappedKeyboardIntent>(mapper.Map(Input(KeyboardKey.Down, KeyboardModifier.None, KeyRepeatState.Repeated))).Intent);
    }

    /// <summary>Proves undeclared keys and modified letters pass through to the native controls.</summary>
    [TestMethod]
    public void MapWhenKeyIsUndeclaredPassesThrough()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Input(KeyboardKey.Tab, KeyboardModifier.None, KeyRepeatState.Initial)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Input(KeyboardKey.Other, KeyboardModifier.None, KeyRepeatState.Initial)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Input(KeyboardKey.J, KeyboardModifier.Control, KeyRepeatState.Initial)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Input(KeyboardKey.F8, KeyboardModifier.None, KeyRepeatState.Initial)));
    }

    /// <summary>Proves movement steps one item and stops at both ends without wrapping.</summary>
    [TestMethod]
    public void MoveWhenItemsExistStepsAndStopsAtBothEnds()
    {
        Assert.AreEqual(1, SettingsFocusOrder.Move(0, 11, UserIntent.MoveNext));
        Assert.AreEqual(10, SettingsFocusOrder.Move(10, 11, UserIntent.MoveNext));
        Assert.AreEqual(4, SettingsFocusOrder.Move(5, 11, UserIntent.MovePrevious));
        Assert.AreEqual(0, SettingsFocusOrder.Move(0, 11, UserIntent.MovePrevious));
        Assert.AreEqual(3, SettingsFocusOrder.Move(3, 11, UserIntent.Confirm));
    }

    /// <summary>Proves an unfocused or stale index starts at the first item and an empty editor has none.</summary>
    [TestMethod]
    public void MoveWhenFocusIsOutsideTheItemsStartsAtTheFirstItem()
    {
        Assert.AreEqual(0, SettingsFocusOrder.Move(-1, 11, UserIntent.MoveNext));
        Assert.AreEqual(0, SettingsFocusOrder.Move(11, 11, UserIntent.MovePrevious));
        Assert.AreEqual(-1, SettingsFocusOrder.Move(-1, 0, UserIntent.MoveNext));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SettingsFocusOrder.Move(0, -1, UserIntent.MoveNext));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => SettingsFocusOrder.Move(0, 1, null!));
    }

    private static UserIntent Mapped(KeyboardIntentMapper mapper, KeyboardKey key)
    {
        return Assert.IsInstanceOfType<MappedKeyboardIntent>(
            mapper.Map(Input(key, KeyboardModifier.None, KeyRepeatState.Initial))).Intent;
    }

    private static KeyboardIntentMapper CreateMapper()
    {
        return new KeyboardIntentMapper(AdjustableClock.Create());
    }

    private static KeyboardInput Input(KeyboardKey key, KeyboardModifier modifier, KeyRepeatState repeat)
    {
        return KeyboardInput.Create(key, modifier, repeat, KeyboardContext.Settings);
    }
}
