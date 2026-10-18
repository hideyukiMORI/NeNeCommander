using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;
using Windows.UI.Core;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves KeyboardTranslation behavior and contracts.</summary>
[TestClass]
public sealed class KeyboardTranslationTests
{
    /// <summary>Proves every supported framework virtual key has one canonical translation.</summary>
    [TestMethod]
    public void TranslateKeyDataWhenVirtualKeyVariesReturnsCanonicalKeyAndRepeatState()
    {
        AssertVirtualKeyIdentities();
        KeyboardInput controlComma = KeyboardInputTranslator.TranslateKeyData(
            188,
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.Control);
        Assert.AreSame(KeyboardKey.Comma, controlComma.Key);
        Assert.AreSame(UserIntent.OpenSettings, Assert.IsInstanceOfType<MappedKeyboardIntent>(
            KeyboardMappingTestContext.CreateMapper().Map(controlComma)).Intent);
        KeyboardMappingTestContext.AssertRawControlKey(VirtualKey.B, KeyboardKey.B, UserIntent.OpenBookmarks);
        KeyboardMappingTestContext.AssertRawControlKey(VirtualKey.Number1, KeyboardKey.One, UserIntent.BookmarkSlotOne);
        KeyboardMappingTestContext.AssertRawControlKey(VirtualKey.Number9, KeyboardKey.Nine, UserIntent.BookmarkSlotNine);
        KeyboardInput controlP = KeyboardInputTranslator.TranslateKeyData(
            (int)VirtualKey.P,
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.Control);
        Assert.AreSame(KeyboardKey.Other, controlP.Key);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(KeyboardMappingTestContext.CreateMapper().Map(controlP));

        KeyboardInput repeated = KeyboardInputTranslator.TranslateKeyData(
            (int)VirtualKey.Down,
            KeyRepeatState.Repeated,
            KeyboardContext.NavigationSurface,
            KeyboardModifier.Alt);
        Assert.AreSame(KeyRepeatState.Repeated, repeated.RepeatState);
        Assert.AreSame(KeyboardContext.NavigationSurface, repeated.Context);
        Assert.AreSame(KeyboardModifier.Alt, repeated.Modifier);
    }

    /// <summary>Proves layout-translated characters and control aliases have exact key identities.</summary>
    [TestMethod]
    public void TranslateCharacterDataWhenCharacterVariesReturnsCanonicalKeyAndRepeatState()
    {
        AssertCharacterIdentities();
        KeyboardInput controlH = KeyboardInputTranslator.TranslateCharacterData(
            '\u0008',
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.Control);
        KeyboardInput unmodifiedBackspaceCharacter = KeyboardInputTranslator.TranslateCharacterData(
            '\u0008',
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.None);

        Assert.AreSame(KeyboardKey.H, controlH.Key);
        Assert.AreSame(KeyboardKey.Other, unmodifiedBackspaceCharacter.Key);

        KeyboardInput repeated = KeyboardInputTranslator.TranslateCharacterData(
            'j',
            KeyRepeatState.Repeated,
            KeyboardContext.TextEntry,
            KeyboardModifier.Control);
        Assert.AreSame(KeyRepeatState.Repeated, repeated.RepeatState);
        Assert.AreSame(KeyboardContext.TextEntry, repeated.Context);
        Assert.AreSame(KeyboardModifier.Control, repeated.Modifier);
    }

    /// <summary>Proves modifier combinations collapse into one closed modifier value.</summary>
    [TestMethod]
    public void TranslateModifierStateWhenKeysVaryReturnsCanonicalModifier()
    {
        Assert.AreSame(
            KeyboardModifier.None,
            KeyboardInputTranslator.TranslateModifierState(CoreVirtualKeyStates.None, CoreVirtualKeyStates.None));
        Assert.AreSame(
            KeyboardModifier.Control,
            KeyboardInputTranslator.TranslateModifierState(CoreVirtualKeyStates.Down, CoreVirtualKeyStates.None));
        Assert.AreSame(
            KeyboardModifier.Alt,
            KeyboardInputTranslator.TranslateModifierState(CoreVirtualKeyStates.None, CoreVirtualKeyStates.Down));
        Assert.AreSame(
            KeyboardModifier.Other,
            KeyboardInputTranslator.TranslateModifierState(CoreVirtualKeyStates.Down, CoreVirtualKeyStates.Down));
    }

    private static void AssertTranslatedVirtualKey(VirtualKey virtualKey, KeyboardKey expected)
    {
        KeyboardInput input = KeyboardInputTranslator.TranslateKeyData(
            (int)virtualKey,
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.None);
        Assert.AreSame(expected, input.Key);
        Assert.AreSame(KeyRepeatState.Initial, input.RepeatState);
    }

    private static void AssertTranslatedCharacter(char character, KeyboardKey expected)
    {
        KeyboardInput input = KeyboardInputTranslator.TranslateCharacterData(
            character,
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.None);
        Assert.AreSame(expected, input.Key);
        Assert.AreSame(KeyRepeatState.Initial, input.RepeatState);
    }

    private static void AssertVirtualKeyIdentities()
    {
        AssertTranslatedVirtualKey(VirtualKey.Down, KeyboardKey.Down);
        AssertTranslatedVirtualKey(VirtualKey.Up, KeyboardKey.Up);
        AssertTranslatedVirtualKey(VirtualKey.Left, KeyboardKey.Left);
        AssertTranslatedVirtualKey(VirtualKey.Right, KeyboardKey.Right);
        AssertTranslatedVirtualKey(VirtualKey.Back, KeyboardKey.Backspace);
        AssertTranslatedVirtualKey(VirtualKey.Enter, KeyboardKey.Enter);
        AssertTranslatedVirtualKey(VirtualKey.PageDown, KeyboardKey.PageDown);
        AssertTranslatedVirtualKey(VirtualKey.PageUp, KeyboardKey.PageUp);
        AssertTranslatedVirtualKey(VirtualKey.Tab, KeyboardKey.Tab);
        AssertTranslatedVirtualKey(VirtualKey.Escape, KeyboardKey.Escape);
        AssertTranslatedVirtualKey(VirtualKey.F2, KeyboardKey.F2);
        AssertTranslatedVirtualKey(VirtualKey.F3, KeyboardKey.F3);
        AssertTranslatedVirtualKey(VirtualKey.F4, KeyboardKey.F4);
        AssertTranslatedVirtualKey(VirtualKey.F5, KeyboardKey.F5);
        AssertTranslatedVirtualKey(VirtualKey.F6, KeyboardKey.F6);
        AssertTranslatedVirtualKey(VirtualKey.F7, KeyboardKey.F7);
        AssertTranslatedVirtualKey(VirtualKey.F8, KeyboardKey.F8);
        AssertTranslatedVirtualKey(VirtualKey.Space, KeyboardKey.Space);
        AssertTranslatedVirtualKey(VirtualKey.A, KeyboardKey.Other);
        AssertTranslatedVirtualKey((VirtualKey)188, KeyboardKey.Other);
    }

    private static void AssertCharacterIdentities()
    {
        AssertTranslatedCharacter(' ', KeyboardKey.Other);
        AssertTranslatedCharacter('d', KeyboardKey.D);
        AssertTranslatedCharacter('b', KeyboardKey.B);
        AssertTranslatedCharacter('\u0002', KeyboardKey.B);
        AssertTranslatedCharacter('\u0004', KeyboardKey.D);
        AssertTranslatedCharacter('G', KeyboardKey.UpperG);
        AssertTranslatedCharacter('g', KeyboardKey.LowerG);
        AssertTranslatedCharacter('h', KeyboardKey.H);
        AssertTranslatedCharacter('j', KeyboardKey.J);
        AssertTranslatedCharacter('k', KeyboardKey.K);
        AssertTranslatedCharacter('l', KeyboardKey.L);
        AssertTranslatedCharacter('\u000c', KeyboardKey.L);
        AssertTranslatedCharacter('p', KeyboardKey.P);
        AssertTranslatedCharacter('\u0010', KeyboardKey.P);
        AssertTranslatedCharacter('r', KeyboardKey.R);
        AssertTranslatedCharacter('\u0012', KeyboardKey.R);
        AssertTranslatedCharacter('u', KeyboardKey.U);
        AssertTranslatedCharacter('\u0015', KeyboardKey.U);
        AssertTranslatedCharacter(',', KeyboardKey.Comma);
        AssertTranslatedCharacter('1', KeyboardKey.One);
        AssertTranslatedCharacter('2', KeyboardKey.Two);
        AssertTranslatedCharacter('3', KeyboardKey.Three);
        AssertTranslatedCharacter('4', KeyboardKey.Four);
        AssertTranslatedCharacter('5', KeyboardKey.Five);
        AssertTranslatedCharacter('6', KeyboardKey.Six);
        AssertTranslatedCharacter('7', KeyboardKey.Seven);
        AssertTranslatedCharacter('8', KeyboardKey.Eight);
        AssertTranslatedCharacter('9', KeyboardKey.Nine);
        AssertTranslatedCharacter('x', KeyboardKey.Other);
    }
}
