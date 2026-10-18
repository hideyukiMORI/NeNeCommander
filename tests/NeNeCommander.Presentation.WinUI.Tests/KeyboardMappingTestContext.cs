using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Creates deterministic inputs for the related projection and input contracts.</summary>
internal static class KeyboardMappingTestContext
{
    internal static void AssertRawControlKey(
        VirtualKey virtualKey,
        KeyboardKey expectedKey,
        UserIntent expectedIntent)
    {
        KeyboardInput input = KeyboardInputTranslator.TranslateKeyData(
            (int)virtualKey,
            KeyRepeatState.Initial,
            KeyboardContext.FileList,
            KeyboardModifier.Control);
        Assert.AreSame(expectedKey, input.Key);
        Assert.AreSame(
            expectedIntent,
            Assert.IsInstanceOfType<MappedKeyboardIntent>(CreateMapper().Map(input)).Intent);
    }

    internal static KeyboardIntentMapper CreateMapper()
    {
        return new KeyboardIntentMapper(AdjustableClock.Create());
    }

    internal static KeyboardInput Input(KeyboardKey key)
    {
        return Input(key, KeyboardModifier.None);
    }

    internal static KeyboardInput Input(KeyboardKey key, KeyboardModifier modifier)
    {
        return KeyboardInput.Create(
            key,
            modifier,
            KeyRepeatState.Initial,
            KeyboardContext.FileList);
    }

    internal static KeyboardInput Input(KeyboardKey key, KeyboardContext context)
    {
        return KeyboardInput.Create(key, KeyboardModifier.None, KeyRepeatState.Initial, context);
    }

    internal static KeyboardInput Repeated(KeyboardKey key, KeyboardContext context)
    {
        return KeyboardInput.Create(key, KeyboardModifier.None, KeyRepeatState.Repeated, context);
    }

    internal static KeyboardInput Input(
        KeyboardKey key,
        KeyboardModifier modifier,
        KeyboardContext context)
    {
        return KeyboardInput.Create(key, modifier, KeyRepeatState.Initial, context);
    }

    internal static void AssertMaps(
        KeyboardIntentMapper mapper,
        KeyboardInput input,
        UserIntent expected)
    {
        MappedKeyboardIntent mapped = Assert.IsInstanceOfType<MappedKeyboardIntent>(mapper.Map(input));
        Assert.AreSame(expected, mapped.Intent);
    }
}
