using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves exclusive ownership of the text-file shortcut and unchanged neighboring commands.</summary>
[TestClass]
public sealed class TextFileKeyboardTests
{
    /// <summary>Proves native translation and both normal contexts expose one initial Ctrl+F7 activation.</summary>
    [TestMethod]
    public void MapWhenTextFileShortcutArrivesMapsOnceAndIgnoresRepeat()
    {
        KeyboardMappingTestContext.AssertRawControlKey(VirtualKey.F7, KeyboardKey.F7, UserIntent.CreateTextFile);
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        foreach (KeyboardContext context in new[] { KeyboardContext.FileList, KeyboardContext.NavigationSurface })
        {
            KeyboardMappingTestContext.AssertMaps(mapper,
                KeyboardMappingTestContext.Input(KeyboardKey.F7, KeyboardModifier.Control, context), UserIntent.CreateTextFile);
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardInput.Create(
                KeyboardKey.F7, KeyboardModifier.Control, KeyRepeatState.Repeated, context)));
            KeyBinding binding = KeyboardIntentMapper.BindingsFor(context).Single(binding => binding.Intent == UserIntent.CreateTextFile);
            Assert.AreEqual("KeyLabelCtrlF7", binding.KeyLabelResourceKey);
        }
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F7), UserIntent.CreateDirectory);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F2), UserIntent.Rename);
    }

    /// <summary>Proves modal, text, palette and other transient owners never emit the file-creation intent.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    [TestProperty("ThreatId", "ADV-016")]
    public void MapWhenAnotherScopeOwnsInputNeverCreatesTextFile()
    {
        IReadOnlyList<KeyboardContext> contexts =
        [
            KeyboardContext.TextEntry, KeyboardContext.AddressEntry, KeyboardContext.Modal,
            KeyboardContext.CommandPalette, KeyboardContext.WindowAdjustment, KeyboardContext.Locations,
            KeyboardContext.Settings, KeyboardContext.KeyboardHelp, KeyboardContext.Bookmarks,
            KeyboardContext.BookmarkSearch, KeyboardContext.BookmarkDraft,
        ];
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        foreach (KeyboardContext context in contexts)
        {
            foreach (KeyRepeatState repeat in new[] { KeyRepeatState.Initial, KeyRepeatState.Repeated })
            {
                KeyboardMappingOutcome outcome = mapper.Map(KeyboardInput.Create(
                    KeyboardKey.F7, KeyboardModifier.Control, repeat, context));
                Assert.IsFalse(outcome is MappedKeyboardIntent mapped && mapped.Intent == UserIntent.CreateTextFile);
            }
        }
    }
}
