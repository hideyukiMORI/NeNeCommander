using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;
using Windows.UI.Core;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves Shift classification is bounded to selection keys in the file list.</summary>
[TestClass]
public sealed class SelectionTranslationTests
{
    /// <summary>Supplies every declared input owner, including native text and menu contexts.</summary>
    public static IEnumerable<object[]> Contexts()
    {
        return typeof(KeyboardContext).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(property => new object[] { Assert.IsInstanceOfType<KeyboardContext>(property.GetValue(null)) });
    }

    /// <summary>Proves F10 retains its rule while only FileList navigation requests new Shift state.</summary>
    [TestMethod]
    [DynamicData(nameof(Contexts))]
    public void RequiresShiftStateWhenContextVariesPreservesOtherOwners(KeyboardContext context)
    {
        foreach (VirtualKey key in new[] { VirtualKey.Up, VirtualKey.Down, VirtualKey.PageUp, VirtualKey.PageDown, VirtualKey.Home, VirtualKey.End })
        {
            Assert.AreEqual(context == KeyboardContext.FileList, KeyboardInputTranslator.RequiresShiftState((int)key, context));
        }
        Assert.IsTrue(KeyboardInputTranslator.RequiresShiftState((int)VirtualKey.F10, context));
        foreach (VirtualKey key in new[] { VirtualKey.Left, VirtualKey.Right, VirtualKey.A, VirtualKey.G,
            VirtualKey.Multiply, VirtualKey.Space, VirtualKey.Tab, VirtualKey.F7, VirtualKey.Application })
        {
            Assert.IsFalse(KeyboardInputTranslator.RequiresShiftState((int)key, context));
        }
        Assert.IsFalse(KeyboardInputTranslator.RequiresShiftState(-1, context));
    }

    /// <summary>Supplies each native text owner and every physical Shift navigation key.</summary>
    public static IEnumerable<object[]> TextNavigationCases()
    {
        KeyboardContext[] contexts = [KeyboardContext.TextEntry, KeyboardContext.AddressEntry,
            KeyboardContext.BookmarkSearch, KeyboardContext.BookmarkDraft];
        VirtualKey[] keys = [VirtualKey.Up, VirtualKey.Down, VirtualKey.PageUp,
            VirtualKey.PageDown, VirtualKey.Home, VirtualKey.End];
        return contexts.SelectMany(context => keys.Select(key => new object[] { context, key }));
    }

    /// <summary>Proves physical Shift navigation preserves native editors and the existing search exit.</summary>
    [TestMethod]
    [DynamicData(nameof(TextNavigationCases))]
    public void TranslateAndMapWhenTextOwnsShiftNavigationRetainsExistingRoutes(KeyboardContext context, VirtualKey key)
    {
        KeyboardModifier modifier = KeyboardInputTranslator.RequiresShiftState((int)key, context)
            ? KeyboardInputTranslator.TranslateShiftModifierState(CoreVirtualKeyStates.None,
                CoreVirtualKeyStates.None, CoreVirtualKeyStates.Down)
            : KeyboardInputTranslator.TranslateModifierState(CoreVirtualKeyStates.None, CoreVirtualKeyStates.None);
        KeyboardInput input = KeyboardInputTranslator.TranslateKeyData((int)key, KeyRepeatState.Initial, context, modifier);
        KeyboardMappingOutcome outcome = KeyboardMappingTestContext.CreateMapper().Map(input);

        Assert.AreSame(KeyboardModifier.None, input.Modifier);
        if (context == KeyboardContext.BookmarkSearch && key == VirtualKey.Down)
        {
            Assert.AreSame(BookmarkManagerKeyAction.LeaveSearch, Assert.IsInstanceOfType<MappedBookmarkManagerAction>(outcome).Action);
        }
        else
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(outcome);
        }
    }

    /// <summary>Proves only FileList produced A aliases request an explicit Shift distinction.</summary>
    [TestMethod]
    [DynamicData(nameof(Contexts))]
    public void RequiresCharacterShiftStateWhenContextVariesPreservesSymbolsAndText(KeyboardContext context)
    {
        foreach (char character in new[] { 'a', 'A', '\u0001' })
        {
            Assert.AreEqual(context == KeyboardContext.FileList, KeyboardInputTranslator.RequiresCharacterShiftState(character, context));
        }
        foreach (char character in new[] { '*', 'G', 'g', 'j', '+', '\u0009', '\u0000', 'あ', '\u0301' })
        {
            Assert.IsFalse(KeyboardInputTranslator.RequiresCharacterShiftState(character, context));
        }
    }

    /// <summary>Proves the produced select-all aliases work but Ctrl+Shift+A cannot become select-all.</summary>
    [TestMethod]
    public void TranslateCharacterDataWhenControlAIncludesShiftRejectsUndeclaredChord()
    {
        KeyboardModifier control = KeyboardInputTranslator.TranslateShiftModifierState(CoreVirtualKeyStates.Down,
            CoreVirtualKeyStates.None, CoreVirtualKeyStates.None);
        KeyboardModifier controlShift = KeyboardInputTranslator.TranslateShiftModifierState(CoreVirtualKeyStates.Down,
            CoreVirtualKeyStates.None, CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);
        Assert.AreSame(KeyboardModifier.Control, control);
        Assert.AreSame(KeyboardModifier.Other, controlShift);

        foreach (char character in new[] { 'a', 'A', '\u0001' })
        {
            KeyboardInput accepted = KeyboardInputTranslator.TranslateCharacterData(character, KeyRepeatState.Initial, KeyboardContext.FileList, control);
            KeyboardInput refused = KeyboardInputTranslator.TranslateCharacterData(character, KeyRepeatState.Initial, KeyboardContext.FileList, controlShift);
            KeyboardMappingTestContext.AssertMaps(KeyboardMappingTestContext.CreateMapper(), accepted, UserIntent.SelectAll);
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(KeyboardMappingTestContext.CreateMapper().Map(refused));
        }
        foreach (char character in new[] { 'A', '\u0001' })
        {
            KeyboardInput native = KeyboardInputTranslator.TranslateCharacterData(character, KeyRepeatState.Initial, KeyboardContext.TextEntry, control);
            Assert.AreSame(KeyboardKey.Other, native.Key);
        }
    }

    /// <summary>Proves letter/multiply precursors remain unmapped and produced symbols own inversion.</summary>
    [TestMethod]
    public void TranslateWhenRawKeysPrecedeProducedCharactersDispatchesOnlyCharacter()
    {
        foreach (VirtualKey key in new[] { VirtualKey.A, VirtualKey.Multiply })
        {
            KeyboardInput raw = KeyboardInputTranslator.TranslateKeyData((int)key, KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.Control);
            Assert.AreSame(KeyboardKey.Other, raw.Key);
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(KeyboardMappingTestContext.CreateMapper().Map(raw));
        }
        KeyboardInput star = KeyboardInputTranslator.TranslateCharacterData('*', KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.None);
        KeyboardMappingTestContext.AssertMaps(KeyboardMappingTestContext.CreateMapper(), star, UserIntent.InvertSelection);
        KeyboardInput upperG = KeyboardInputTranslator.TranslateCharacterData('G', KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.None);
        KeyboardMappingTestContext.AssertMaps(KeyboardMappingTestContext.CreateMapper(), upperG, UserIntent.FocusLast);
        KeyboardInput controlCodeWithoutControl = KeyboardInputTranslator.TranslateCharacterData('\u0001', KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.None);
        Assert.AreSame(KeyboardKey.Other, controlCodeWithoutControl.Key);
    }

    /// <summary>Proves Home and End have distinct translated identities without adding plain bindings.</summary>
    [TestMethod]
    public void TranslateKeyDataWhenHomeOrEndIsPressedRetainsRepeatAndContext()
    {
        foreach ((VirtualKey key, KeyboardKey expected) in new[] { (VirtualKey.Home, KeyboardKey.Home), (VirtualKey.End, KeyboardKey.End) })
        {
            KeyboardInput translated = KeyboardInputTranslator.TranslateKeyData((int)key, KeyRepeatState.Repeated, KeyboardContext.FileList, KeyboardModifier.Shift);
            Assert.AreSame(expected, translated.Key);
            Assert.AreSame(KeyRepeatState.Repeated, translated.RepeatState);
            Assert.AreSame(KeyboardModifier.Shift, translated.Modifier);
            Assert.AreSame(KeyboardContext.FileList, translated.Context);
        }
    }
}
