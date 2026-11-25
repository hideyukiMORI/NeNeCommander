using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves layout-translated filter entry and native editor ownership through the sole mapper.</summary>
[TestClass]
public sealed class PaneFilterKeyboardTests
{
    /// <summary>Proves only a produced slash in the file list opens filtering; raw or modified input does not.</summary>
    [TestMethod]
    public void MapWhenSlashIsProducedOpensOnlyFromFileList()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardInput raw = KeyboardInputTranslator.TranslateKeyData(191, KeyRepeatState.Initial,
            KeyboardContext.FileList, KeyboardModifier.None);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(raw));
        KeyboardInput produced = KeyboardInputTranslator.TranslateCharacterData('/', KeyRepeatState.Initial,
            KeyboardContext.FileList, KeyboardModifier.None);
        KeyboardMappingTestContext.AssertMaps(mapper, produced, UserIntent.OpenPaneFilter);
        KeyBinding binding = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList)
            .Single(item => item.Intent == UserIntent.OpenPaneFilter);
        Assert.AreEqual("KeyLabelSlash", binding.KeyLabelResourceKey);
        foreach (KeyboardContext context in new[] { KeyboardContext.PaneFilter, KeyboardContext.TextEntry,
            KeyboardContext.AddressEntry, KeyboardContext.NavigationSurface, KeyboardContext.CommandEntry })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(KeyboardMappingTestContext.CreateMapper().Map(
                KeyboardInputTranslator.TranslateCharacterData('/', KeyRepeatState.Initial, context, KeyboardModifier.None)));
        }
        foreach (KeyboardModifier modifier in new[] { KeyboardModifier.Control, KeyboardModifier.Alt, KeyboardModifier.Other })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(
                KeyboardInputTranslator.TranslateCharacterData('/', KeyRepeatState.Initial, KeyboardContext.FileList, modifier)));
        }
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardInputTranslator.TranslateKeyData(
            (int)VirtualKey.F, KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.Control)));
    }

    /// <summary>Proves a held opener cannot type extra slash characters into the newly focused editor.</summary>
    [TestMethod]
    public void MapWhenOpenerRepeatsAcrossContextChangeConsumesUntilFreshPress()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Slash), UserIntent.OpenPaneFilter);
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
            KeyboardMappingTestContext.Repeated(KeyboardKey.Slash, KeyboardContext.PaneFilter)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(
            KeyboardMappingTestContext.Input(KeyboardKey.Slash, KeyboardContext.PaneFilter)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(KeyboardMappingTestContext.CreateMapper().Map(
            KeyboardMappingTestContext.Repeated(KeyboardKey.Slash, KeyboardContext.FileList)));
    }

    /// <summary>Proves completion/cancellation cannot replay as file-list open or selection clearing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public void MapWhenEditorExitRepeatsKeepsOriginalPressOwnership()
    {
        foreach (KeyboardKey key in new[] { KeyboardKey.Enter, KeyboardKey.Escape })
        {
            UserIntent expected = key == KeyboardKey.Enter ? UserIntent.Confirm : UserIntent.Escape;
            foreach (KeyboardModifier modifier in new[] { KeyboardModifier.None, KeyboardModifier.Control,
                KeyboardModifier.Alt, KeyboardModifier.Other })
            {
                KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
                KeyboardMappingTestContext.AssertMaps(mapper,
                    KeyboardMappingTestContext.Input(key, modifier, KeyboardContext.PaneFilter), expected);
                _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
                    KeyboardMappingTestContext.Repeated(key, KeyboardContext.FileList)));
                _ = Assert.IsInstanceOfType<KeyboardConsumed>(KeyboardMappingTestContext.CreateMapper().Map(
                    KeyboardMappingTestContext.Repeated(key, KeyboardContext.PaneFilter)));
            }
        }
    }

    /// <summary>Proves native committed text and selection remain native, including IME dismissal ownership.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public void MapWhenNativeTextOrImeOwnsInputDoesNotExecuteFileCommands()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        foreach (char character in new[] { 'a', '*', '/', 'j', 'k', 'h', 'l', ' ', '\u0001' })
        {
            KeyboardModifier modifier = character == '\u0001' ? KeyboardModifier.Control : KeyboardModifier.None;
            KeyboardInput input = KeyboardInputTranslator.TranslateCharacterData(character, KeyRepeatState.Initial,
                KeyboardContext.PaneFilter, modifier);
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(input));
        }
        foreach (VirtualKey key in new[] { VirtualKey.Space, VirtualKey.Tab, VirtualKey.Up, VirtualKey.Down,
            VirtualKey.F5, VirtualKey.Delete })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardInputTranslator.TranslateKeyData(
                (int)key, KeyRepeatState.Initial, KeyboardContext.PaneFilter, KeyboardModifier.None)));
        }
        mapper.ObserveNativeInput(KeyboardMappingTestContext.Input(KeyboardKey.Escape, KeyboardContext.PaneFilter));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
            KeyboardMappingTestContext.Repeated(KeyboardKey.Escape, KeyboardContext.PaneFilter)));
        KeyboardMappingTestContext.AssertMaps(mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.Escape, KeyboardContext.PaneFilter), UserIntent.Escape);
    }

    /// <summary>Proves opening filtering cancels a pending gg without triggering focus movement.</summary>
    [TestMethod]
    public void MapWhenSlashFollowsChordPrefixCancelsPrefix()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Slash), UserIntent.OpenPaneFilter);
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
    }
}
