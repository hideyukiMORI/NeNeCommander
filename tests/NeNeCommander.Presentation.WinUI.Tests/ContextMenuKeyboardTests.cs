using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves native menu input uses the canonical mapping and shared physical-press guard.</summary>
[TestClass]
public sealed class ContextMenuKeyboardTests
{
    /// <summary>Proves both opening keys map once with declared hints while bare F10 retains focus navigation.</summary>
    [TestMethod]
    public void MapWhenOpeningKeysAreInitialUsesExistingContextIntent()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardMappingTestContext.AssertMaps(mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.F10, KeyboardModifier.Shift), UserIntent.OpenContextMenu);
        KeyboardInput apps = KeyboardInputTranslator.TranslateKeyData((int)VirtualKey.Application,
            KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.None);
        Assert.AreSame(KeyboardKey.Apps, apps.Key);
        KeyboardMappingTestContext.AssertMaps(mapper, apps, UserIntent.OpenContextMenu);
        _ = Assert.IsInstanceOfType<MappedKeyboardFocusAction>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F10)));
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Tab), UserIntent.ActivateOtherPane);
        string[] hints = [.. KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList)
            .Where(binding => binding.Intent == UserIntent.OpenContextMenu).Select(binding => binding.KeyLabelResourceKey)];
        string[] expected = ["KeyLabelShiftF10", "KeyLabelApps"];
        CollectionAssert.AreEquivalent(expected, hints);
    }

    /// <summary>Proves a held opener cannot reopen after dismissal or leak into another input owner.</summary>
    [TestMethod]
    public void MapWhenOpenerRepeatsConsumesInMenuAndReturnedPane()
    {
        foreach (KeyboardKey key in new[] { KeyboardKey.Apps, KeyboardKey.F10 })
        {
            KeyboardModifier modifier = key == KeyboardKey.F10 ? KeyboardModifier.Shift : KeyboardModifier.None;
            KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
            _ = mapper.Map(KeyboardMappingTestContext.Input(key, modifier));
            foreach (KeyboardContext context in new[] { KeyboardContext.ContextMenu, KeyboardContext.FileList, KeyboardContext.TextEntry })
            {
                _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
                    KeyboardInput.Create(key, modifier, KeyRepeatState.Repeated, context)));
            }
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(KeyboardMappingTestContext.CreateMapper().Map(
                KeyboardInput.Create(key, modifier, KeyRepeatState.Repeated, KeyboardContext.FileList)));
            KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(key, modifier), UserIntent.OpenContextMenu);
        }
    }

    /// <summary>Proves native activation and submenu/root Escape retain one press across owner changes.</summary>
    [TestMethod]
    public void MapWhenNativeMenuKeysRepeatKeepsSingleShotOwnership()
    {
        foreach (KeyboardKey key in new[] { KeyboardKey.Enter, KeyboardKey.Space, KeyboardKey.Escape })
        {
            KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(key, KeyboardContext.ContextMenu)));
            _ = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Down, KeyboardContext.ContextMenu));
            foreach (KeyboardContext context in new[] { KeyboardContext.ContextMenu, KeyboardContext.Modal, KeyboardContext.FileList })
            {
                _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardMappingTestContext.Repeated(key, context)));
            }
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(key, KeyboardContext.ContextMenu)));
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(KeyboardMappingTestContext.CreateMapper().Map(
                KeyboardMappingTestContext.Repeated(key, KeyboardContext.ContextMenu)));
        }
    }

    /// <summary>Proves native menu navigation passes through and no file command is dispatched.</summary>
    [TestMethod]
    public void MapWhenMenuOwnsInputDoesNotDispatchPaneCommands()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        foreach (KeyboardKey key in new[] { KeyboardKey.Up, KeyboardKey.Down, KeyboardKey.Left, KeyboardKey.Right,
            KeyboardKey.J, KeyboardKey.F2, KeyboardKey.F5, KeyboardKey.F6, KeyboardKey.F7, KeyboardKey.F8, KeyboardKey.Other })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(key, KeyboardContext.ContextMenu)));
        }
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Apps, KeyboardContext.ContextMenu)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F10, KeyboardContext.ContextMenu)));
    }

    /// <summary>Proves context entry is refused by text/modal owners and undeclared modifiers.</summary>
    [TestMethod]
    public void MapWhenAnotherOwnerOrModifierIsPresentDoesNotOpenContext()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        foreach (KeyboardContext context in new[] { KeyboardContext.TextEntry, KeyboardContext.Modal, KeyboardContext.CommandEntry,
            KeyboardContext.BookmarkDraft, KeyboardContext.CommandPalette, KeyboardContext.Settings, KeyboardContext.AddressEntry,
            KeyboardContext.Bookmarks, KeyboardContext.KeyboardHelp, KeyboardContext.Locations, KeyboardContext.WindowAdjustment })
        {
            foreach (KeyboardKey key in new[] { KeyboardKey.Apps, KeyboardKey.F10 })
            {
                KeyboardMappingOutcome outcome = mapper.Map(KeyboardMappingTestContext.Input(key,
                    key == KeyboardKey.F10 ? KeyboardModifier.Shift : KeyboardModifier.None, context));
                Assert.IsFalse(outcome is MappedKeyboardIntent mapped && mapped.Intent == UserIntent.OpenContextMenu);
            }
        }
        foreach (KeyboardModifier modifier in new[] { KeyboardModifier.Control, KeyboardModifier.Alt, KeyboardModifier.Other })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Apps, modifier)));
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F10, modifier)));
        }
    }
}
