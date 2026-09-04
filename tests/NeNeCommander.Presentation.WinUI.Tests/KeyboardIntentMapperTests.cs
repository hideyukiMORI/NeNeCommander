using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;
using Windows.UI.Core;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the context-aware canonical Vim keyboard map.</summary>
[TestClass]
public sealed class KeyboardIntentMapperTests
{
    /// <summary>Proves every unmodified file-list binding.</summary>
    [TestMethod]
    public void MapWhenSingleFileListKeysAreUsedEmitsCanonicalIntents()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        AssertMaps(mapper, Input(KeyboardKey.J), UserIntent.MoveNext);
        AssertMaps(mapper, Input(KeyboardKey.Down), UserIntent.MoveNext);
        AssertMaps(mapper, Input(KeyboardKey.K), UserIntent.MovePrevious);
        AssertMaps(mapper, Input(KeyboardKey.Up), UserIntent.MovePrevious);
        AssertMaps(mapper, Input(KeyboardKey.H), UserIntent.NavigateParent);
        AssertMaps(mapper, Input(KeyboardKey.Backspace), UserIntent.NavigateParent);
        AssertMaps(mapper, Input(KeyboardKey.L), UserIntent.OpenFocused);
        AssertMaps(mapper, Input(KeyboardKey.Enter), UserIntent.OpenFocused);
        AssertMaps(mapper, Input(KeyboardKey.UpperG), UserIntent.FocusLast);
        AssertMaps(mapper, Input(KeyboardKey.PageDown), UserIntent.MoveHalfPageDown);
        AssertMaps(mapper, Input(KeyboardKey.PageUp), UserIntent.MoveHalfPageUp);
        AssertMaps(mapper, Input(KeyboardKey.Tab), UserIntent.ActivateOtherPane);
        AssertMaps(mapper, Input(KeyboardKey.Space), UserIntent.ToggleSelection);
        AssertMaps(mapper, Input(KeyboardKey.Escape), UserIntent.Escape);
        AssertMaps(mapper, Input(KeyboardKey.F2), UserIntent.Rename);
        AssertMaps(mapper, Input(KeyboardKey.F5), UserIntent.Copy);
        AssertMaps(mapper, Input(KeyboardKey.F6), UserIntent.Move);
        AssertMaps(mapper, Input(KeyboardKey.F7), UserIntent.CreateDirectory);
        AssertMaps(mapper, Input(KeyboardKey.F8), UserIntent.Delete);
    }

    /// <summary>Proves every modified Windows-compatible alias.</summary>
    [TestMethod]
    public void MapWhenModifiedAliasesAreUsedEmitsCanonicalIntents()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        AssertMaps(mapper, Input(KeyboardKey.Up, KeyboardModifier.Alt), UserIntent.NavigateParent);
        AssertMaps(mapper, Input(KeyboardKey.D, KeyboardModifier.Control), UserIntent.MoveHalfPageDown);
        AssertMaps(mapper, Input(KeyboardKey.U, KeyboardModifier.Control), UserIntent.MoveHalfPageUp);
        AssertMaps(mapper, Input(KeyboardKey.L, KeyboardModifier.Control), UserIntent.FocusAddress);
        AssertMaps(mapper, Input(KeyboardKey.R, KeyboardModifier.Control), UserIntent.Refresh);
    }

    /// <summary>Proves the gg chord includes its exact lifetime boundary.</summary>
    [TestMethod]
    public void MapWhenGgCompletesWithinLifetimeFocusFirstIntent()
    {
        AdjustableClock clock = AdjustableClock.Create();
        KeyboardIntentMapper mapper = new(clock);

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));
        clock.Advance(TimeSpan.FromMilliseconds(750));
        AssertMaps(mapper, Input(KeyboardKey.LowerG), UserIntent.FocusFirst);
    }

    /// <summary>Proves an expired gg suffix begins a fresh chord.</summary>
    [TestMethod]
    public void MapWhenGgExpiresCurrentGStartsNewChord()
    {
        AdjustableClock clock = AdjustableClock.Create();
        KeyboardIntentMapper mapper = new(clock);

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));
        clock.Advance(TimeSpan.FromMilliseconds(751));

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));
    }

    /// <summary>Proves an unrelated suffix is processed normally.</summary>
    [TestMethod]
    public void MapWhenUnrelatedKeyFollowsGCancelsChordAndProcessesKey()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));

        AssertMaps(mapper, Input(KeyboardKey.J), UserIntent.MoveNext);
    }

    /// <summary>Proves the raw virtual-key event of a printable key neither completes nor cancels a chord.</summary>
    [TestMethod]
    public void MapWhenUnmappedKeyFollowsGKeepsChordPending()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Input(KeyboardKey.Other)));

        AssertMaps(mapper, Input(KeyboardKey.LowerG), UserIntent.FocusFirst);
    }

    /// <summary>Proves text and modal contexts block underlying commands.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public void MapWhenTextEntryOrModalOwnsInputBlocksUnderlyingCommands()
    {
        KeyboardIntentMapper mapper = CreateMapper();
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));

        KeyboardMappingOutcome text = mapper.Map(Input(KeyboardKey.F8, KeyboardContext.TextEntry));
        KeyboardMappingOutcome modal = mapper.Map(Input(KeyboardKey.F8, KeyboardContext.Modal));
        KeyboardMappingOutcome textEscape = mapper.Map(Input(KeyboardKey.Escape, KeyboardContext.TextEntry));
        KeyboardMappingOutcome modalEscape = mapper.Map(Input(KeyboardKey.Escape, KeyboardContext.Modal));
        KeyboardMappingOutcome modalEnter = mapper.Map(Input(KeyboardKey.Enter, KeyboardContext.Modal));
        KeyboardMappingOutcome textEnter = mapper.Map(Input(KeyboardKey.Enter, KeyboardContext.TextEntry));
        KeyboardMappingOutcome modalMovement = mapper.Map(Input(KeyboardKey.J, KeyboardContext.Modal));

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(text);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(modal);
        Assert.AreSame(UserIntent.Escape, Assert.IsInstanceOfType<MappedKeyboardIntent>(textEscape).Intent);
        Assert.AreSame(UserIntent.Escape, Assert.IsInstanceOfType<MappedKeyboardIntent>(modalEscape).Intent);
        Assert.AreSame(UserIntent.Confirm, Assert.IsInstanceOfType<MappedKeyboardIntent>(modalEnter).Intent);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(textEnter);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(modalMovement);
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(Input(KeyboardKey.LowerG)));
    }

    /// <summary>Proves repeated destructive keys do not emit commands.</summary>
    [TestMethod]
    public void MapWhenDestructiveKeyRepeatsPassesThroughWithoutIntent()
    {
        KeyboardIntentMapper mapper = CreateMapper();
        KeyboardKey[] keys = [KeyboardKey.F2, KeyboardKey.F5, KeyboardKey.F6, KeyboardKey.F7, KeyboardKey.F8];

        foreach (KeyboardKey key in keys)
        {
            KeyboardMappingOutcome outcome = mapper.Map(
                KeyboardInput.Create(
                    key,
                    KeyboardModifier.None,
                    KeyRepeatState.Repeated,
                    KeyboardContext.FileList));
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(outcome);
        }
    }

    /// <summary>Proves repeated movement remains responsive.</summary>
    [TestMethod]
    public void MapWhenMovementKeyRepeatsEmitsMovementIntent()
    {
        KeyboardIntentMapper mapper = CreateMapper();
        KeyboardInput input = KeyboardInput.Create(
            KeyboardKey.J,
            KeyboardModifier.None,
            KeyRepeatState.Repeated,
            KeyboardContext.FileList);

        AssertMaps(mapper, input, UserIntent.MoveNext);
    }

    /// <summary>Proves F5 meaning is selected by explicit context.</summary>
    [TestMethod]
    public void MapWhenNavigationSurfaceReceivesF5RefreshesWithoutCopying()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        AssertMaps(mapper, Input(KeyboardKey.F5, KeyboardContext.NavigationSurface), UserIntent.Refresh);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(Input(KeyboardKey.J, KeyboardContext.NavigationSurface)));
    }

    /// <summary>Proves unmapped keys and modifier combinations pass through.</summary>
    [TestMethod]
    public void MapWhenKeyOrModifierIsUnmappedPassesThrough()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Input(KeyboardKey.Other)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(Input(KeyboardKey.J, KeyboardModifier.Other)));
    }

    /// <summary>Proves every supported framework virtual key has one canonical translation.</summary>
    [TestMethod]
    public void TranslateKeyDataWhenVirtualKeyVariesReturnsCanonicalKeyAndRepeatState()
    {
        AssertTranslatedVirtualKey(VirtualKey.Down, KeyboardKey.Down);
        AssertTranslatedVirtualKey(VirtualKey.Up, KeyboardKey.Up);
        AssertTranslatedVirtualKey(VirtualKey.Back, KeyboardKey.Backspace);
        AssertTranslatedVirtualKey(VirtualKey.Enter, KeyboardKey.Enter);
        AssertTranslatedVirtualKey(VirtualKey.PageDown, KeyboardKey.PageDown);
        AssertTranslatedVirtualKey(VirtualKey.PageUp, KeyboardKey.PageUp);
        AssertTranslatedVirtualKey(VirtualKey.Tab, KeyboardKey.Tab);
        AssertTranslatedVirtualKey(VirtualKey.Escape, KeyboardKey.Escape);
        AssertTranslatedVirtualKey(VirtualKey.F2, KeyboardKey.F2);
        AssertTranslatedVirtualKey(VirtualKey.F5, KeyboardKey.F5);
        AssertTranslatedVirtualKey(VirtualKey.F6, KeyboardKey.F6);
        AssertTranslatedVirtualKey(VirtualKey.F7, KeyboardKey.F7);
        AssertTranslatedVirtualKey(VirtualKey.F8, KeyboardKey.F8);
        AssertTranslatedVirtualKey(VirtualKey.Space, KeyboardKey.Space);
        AssertTranslatedVirtualKey(VirtualKey.A, KeyboardKey.Other);

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
        AssertTranslatedCharacter(' ', KeyboardKey.Other);
        AssertTranslatedCharacter('d', KeyboardKey.D);
        AssertTranslatedCharacter('\u0004', KeyboardKey.D);
        AssertTranslatedCharacter('G', KeyboardKey.UpperG);
        AssertTranslatedCharacter('g', KeyboardKey.LowerG);
        AssertTranslatedCharacter('h', KeyboardKey.H);
        AssertTranslatedCharacter('j', KeyboardKey.J);
        AssertTranslatedCharacter('k', KeyboardKey.K);
        AssertTranslatedCharacter('l', KeyboardKey.L);
        AssertTranslatedCharacter('\u000c', KeyboardKey.L);
        AssertTranslatedCharacter('r', KeyboardKey.R);
        AssertTranslatedCharacter('\u0012', KeyboardKey.R);
        AssertTranslatedCharacter('u', KeyboardKey.U);
        AssertTranslatedCharacter('\u0015', KeyboardKey.U);
        AssertTranslatedCharacter('x', KeyboardKey.Other);

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

    /// <summary>Proves no context maps one keystroke to more than one intent (KBD-005).</summary>
    [TestMethod]
    public void BindingsForWhenContextIsDeclaredMapsEachKeystrokeToOneIntent()
    {
        KeyboardContext[] contexts =
        [
            KeyboardContext.FileList,
            KeyboardContext.NavigationSurface,
            KeyboardContext.Modal,
            KeyboardContext.TextEntry,
        ];

        foreach (KeyboardContext context in contexts)
        {
            AssertKeystrokesAreUnique(context);
        }
    }

    /// <summary>Proves every declared binding is the intent the mapper emits for its keystroke.</summary>
    [TestMethod]
    public void BindingsForWhenDeclarationIsMappedEmitsTheDeclaredIntent()
    {
        KeyboardContext[] contexts =
        [
            KeyboardContext.FileList,
            KeyboardContext.NavigationSurface,
            KeyboardContext.Modal,
            KeyboardContext.TextEntry,
        ];

        foreach (KeyboardContext context in contexts)
        {
            AssertDeclarationsAreMapped(context);
        }
    }

    /// <summary>Proves the file-list context declares every documented binding and nothing else.</summary>
    [TestMethod]
    public void BindingsForWhenContextIsFileListDeclaresTheDocumentedCount()
    {
        Assert.HasCount(24, KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList));
        Assert.HasCount(6, KeyboardIntentMapper.BindingsFor(KeyboardContext.NavigationSurface));
        Assert.HasCount(2, KeyboardIntentMapper.BindingsFor(KeyboardContext.Modal));
        Assert.HasCount(1, KeyboardIntentMapper.BindingsFor(KeyboardContext.TextEntry));
    }

    /// <summary>Proves every key identity names one distinct key-cap label resource.</summary>
    [TestMethod]
    public void LabelResourceKeyWhenKeyIsReadNamesOneDistinctResource()
    {
        Assert.AreEqual("KeyLabelLowerG", KeyboardKey.LowerG.LabelResourceKey);
        Assert.AreEqual("KeyLabelUpperG", KeyboardKey.UpperG.LabelResourceKey);
        Assert.AreEqual("KeyLabelH", KeyboardKey.H.LabelResourceKey);
        Assert.AreEqual("KeyLabelJ", KeyboardKey.J.LabelResourceKey);
        Assert.AreEqual("KeyLabelK", KeyboardKey.K.LabelResourceKey);
        Assert.AreEqual("KeyLabelL", KeyboardKey.L.LabelResourceKey);
        Assert.AreEqual("KeyLabelD", KeyboardKey.D.LabelResourceKey);
        Assert.AreEqual("KeyLabelR", KeyboardKey.R.LabelResourceKey);
        Assert.AreEqual("KeyLabelU", KeyboardKey.U.LabelResourceKey);
        Assert.AreEqual("KeyLabelDown", KeyboardKey.Down.LabelResourceKey);
        Assert.AreEqual("KeyLabelUp", KeyboardKey.Up.LabelResourceKey);
        Assert.AreEqual("KeyLabelBackspace", KeyboardKey.Backspace.LabelResourceKey);
        Assert.AreEqual("KeyLabelEnter", KeyboardKey.Enter.LabelResourceKey);
        Assert.AreEqual("KeyLabelPageDown", KeyboardKey.PageDown.LabelResourceKey);
        Assert.AreEqual("KeyLabelPageUp", KeyboardKey.PageUp.LabelResourceKey);
        Assert.AreEqual("KeyLabelTab", KeyboardKey.Tab.LabelResourceKey);
        Assert.AreEqual("KeyLabelSpace", KeyboardKey.Space.LabelResourceKey);
        Assert.AreEqual("KeyLabelEscape", KeyboardKey.Escape.LabelResourceKey);
        Assert.AreEqual("KeyLabelF2", KeyboardKey.F2.LabelResourceKey);
        Assert.AreEqual("KeyLabelF5", KeyboardKey.F5.LabelResourceKey);
        Assert.AreEqual("KeyLabelF6", KeyboardKey.F6.LabelResourceKey);
        Assert.AreEqual("KeyLabelF7", KeyboardKey.F7.LabelResourceKey);
        Assert.AreEqual("KeyLabelF8", KeyboardKey.F8.LabelResourceKey);
        Assert.AreEqual("KeyLabelUnmapped", KeyboardKey.Other.LabelResourceKey);
        AssertLabelResourceKeysAreDistinct();
    }

    /// <summary>Proves the binding query rejects an absent context.</summary>
    [TestMethod]
    public void BindingsForWhenContextIsNullThrowsArgumentNullException()
    {
        MethodInfo method = typeof(KeyboardIntentMapper).GetMethod(
            nameof(KeyboardIntentMapper.BindingsFor),
            BindingFlags.Public | BindingFlags.Static) ??
            throw new AssertFailedException("The binding query was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static void AssertKeystrokesAreUnique(KeyboardContext context)
    {
        HashSet<string> keystrokes = [];
        foreach (KeyBinding binding in KeyboardIntentMapper.BindingsFor(context))
        {
            Assert.AreSame(context, binding.Context);
            Assert.IsTrue(
                keystrokes.Add(binding.Key.LabelResourceKey + " " + binding.Modifier.GetType().Name),
                "A keystroke is declared more than once in one context.");
        }
    }

    private static void AssertDeclarationsAreMapped(KeyboardContext context)
    {
        foreach (KeyBinding binding in KeyboardIntentMapper.BindingsFor(context))
        {
            KeyboardInput input = KeyboardInput.Create(
                binding.Key,
                binding.Modifier,
                KeyRepeatState.Initial,
                context);
            MappedKeyboardIntent mapped = Assert.IsInstanceOfType<MappedKeyboardIntent>(
                CreateMapper().Map(input));
            Assert.AreSame(binding.Intent, mapped.Intent);
        }
    }

    private static void AssertLabelResourceKeysAreDistinct()
    {
        HashSet<string> labels = [];
        foreach (KeyboardContext context in new[] { KeyboardContext.FileList, KeyboardContext.NavigationSurface })
        {
            foreach (KeyBinding binding in KeyboardIntentMapper.BindingsFor(context))
            {
                _ = labels.Add(binding.Key.LabelResourceKey);
                Assert.StartsWith("KeyLabel", binding.Key.LabelResourceKey);
            }
        }
        Assert.IsNotEmpty(labels);
    }

    private static KeyboardIntentMapper CreateMapper()
    {
        return new KeyboardIntentMapper(AdjustableClock.Create());
    }

    private static KeyboardInput Input(KeyboardKey key)
    {
        return Input(key, KeyboardModifier.None);
    }

    private static KeyboardInput Input(KeyboardKey key, KeyboardModifier modifier)
    {
        return KeyboardInput.Create(
            key,
            modifier,
            KeyRepeatState.Initial,
            KeyboardContext.FileList);
    }

    private static KeyboardInput Input(KeyboardKey key, KeyboardContext context)
    {
        return KeyboardInput.Create(key, KeyboardModifier.None, KeyRepeatState.Initial, context);
    }

    private static void AssertMaps(
        KeyboardIntentMapper mapper,
        KeyboardInput input,
        UserIntent expected)
    {
        MappedKeyboardIntent mapped = Assert.IsInstanceOfType<MappedKeyboardIntent>(mapper.Map(input));
        Assert.AreSame(expected, mapped.Intent);
    }
}
