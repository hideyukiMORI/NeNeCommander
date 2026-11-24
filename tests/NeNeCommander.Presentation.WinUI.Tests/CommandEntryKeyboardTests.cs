using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Help;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;
using Windows.UI.Core;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves command-button focus shares canonical translation, mapping, hints and repeat ownership.</summary>
[TestClass]
public sealed class CommandEntryKeyboardTests
{
    /// <summary>Proves entry and both return keys change only focus, while pane Tab still switches panes.</summary>
    [TestMethod]
    public void MapWhenEnteringAndLeavingGroupUsesFocusActions()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        MappedKeyboardFocusAction entry = Assert.IsInstanceOfType<MappedKeyboardFocusAction>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F10)));
        Assert.AreSame(KeyboardFocusAction.CommandEntry, entry.Action);
        foreach (KeyboardKey key in new[] { KeyboardKey.F10, KeyboardKey.Escape })
        {
            MappedKeyboardFocusAction returned = Assert.IsInstanceOfType<MappedKeyboardFocusAction>(
                mapper.Map(KeyboardMappingTestContext.Input(key, KeyboardContext.CommandEntry)));
            Assert.AreSame(KeyboardFocusAction.ActivePane, returned.Action);
        }
        KeyboardMappingTestContext.AssertMaps(mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.Tab), UserIntent.ActivateOtherPane);
    }

    /// <summary>Proves native navigation and activation pass through without mapping pane actions.</summary>
    [TestMethod]
    public void MapWhenCommandEntryOwnsInputDoesNotMapFileCommands()
    {
        KeyboardKey[] keys = [KeyboardKey.Tab, KeyboardKey.Enter, KeyboardKey.Space,
            KeyboardKey.J, KeyboardKey.K, KeyboardKey.F2, KeyboardKey.F5, KeyboardKey.F6, KeyboardKey.F7, KeyboardKey.F8];
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        foreach (KeyboardKey key in keys)
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(
                KeyboardMappingTestContext.Input(key, KeyboardContext.CommandEntry)));
        }
        KeyboardMappingTestContext.AssertMaps(mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.B, KeyboardModifier.Control, KeyboardContext.CommandEntry), UserIntent.OpenBookmarks);
        KeyboardMappingTestContext.AssertMaps(mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.Comma, KeyboardModifier.Control, KeyboardContext.CommandEntry), UserIntent.OpenSettings);
        KeyboardMappingTestContext.AssertMaps(mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.F1, KeyboardContext.CommandEntry), UserIntent.OpenHelp);
    }

    /// <summary>Proves held focus keys remain consumed across context changes until a new press.</summary>
    [TestMethod]
    public void MapWhenFocusKeysRepeatAcrossContextsConsumesWithoutReenteringOrClearingSelection()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        _ = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F10));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
            KeyboardMappingTestContext.Repeated(KeyboardKey.F10, KeyboardContext.CommandEntry)));
        _ = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Escape, KeyboardContext.CommandEntry));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
            KeyboardMappingTestContext.Repeated(KeyboardKey.Escape, KeyboardContext.FileList)));
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Escape), UserIntent.Escape);
    }

    /// <summary>Proves native button activation shares the guard before a modal or returned pane receives repeats.</summary>
    [TestMethod]
    public void MapWhenNativeButtonActivationRepeatsConsumesAcrossOwners()
    {
        foreach (KeyboardKey key in new[] { KeyboardKey.Enter, KeyboardKey.Space })
        {
            KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(
                KeyboardMappingTestContext.Input(key, KeyboardContext.CommandEntry)));
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
                KeyboardMappingTestContext.Repeated(key, KeyboardContext.Settings)));
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
                KeyboardMappingTestContext.Repeated(key, KeyboardContext.FileList)));
        }
    }

    /// <summary>Proves Shift+F10 carries its declared context-menu modifier without changing produced characters.</summary>
    [TestMethod]
    public void TranslateWhenF10IsShiftedPreservesContextMenuModifier()
    {
        KeyboardModifier shift = KeyboardInputTranslator.TranslateF10ModifierState(
            CoreVirtualKeyStates.None, CoreVirtualKeyStates.None, CoreVirtualKeyStates.Down);
        Assert.AreSame(KeyboardModifier.Shift, shift);
        Assert.AreSame(KeyboardModifier.Other, KeyboardInputTranslator.TranslateF10ModifierState(
            CoreVirtualKeyStates.Down, CoreVirtualKeyStates.None, CoreVirtualKeyStates.Down));
        Assert.AreSame(KeyboardModifier.Other, KeyboardInputTranslator.TranslateF10ModifierState(
            CoreVirtualKeyStates.None, CoreVirtualKeyStates.Down, CoreVirtualKeyStates.Down));
        Assert.AreSame(KeyboardModifier.None, KeyboardInputTranslator.TranslateF10ModifierState(
            CoreVirtualKeyStates.None, CoreVirtualKeyStates.None, CoreVirtualKeyStates.Locked));
        KeyboardInput input = KeyboardInputTranslator.TranslateKeyData((int)VirtualKey.F10,
            KeyRepeatState.Initial, KeyboardContext.FileList, shift);
        Assert.AreSame(KeyboardKey.F10, input.Key);
        KeyboardMappingTestContext.AssertMaps(KeyboardMappingTestContext.CreateMapper(), input, UserIntent.OpenContextMenu);
        Assert.AreSame(KeyboardKey.UpperG, KeyboardInputTranslator.TranslateCharacterData('G',
            KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.None).Key);
        Assert.AreSame(KeyboardKey.Plus, KeyboardInputTranslator.TranslateCharacterData('+',
            KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.None).Key);
    }

    /// <summary>Proves entry is neither available in modal contexts nor a continuation of a pending gg chord.</summary>
    [TestMethod]
    public void MapWhenFocusBindingInterruptsChordDoesNotLeakIntoOtherOwners()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        _ = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG));
        _ = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F10));
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
        KeyboardContext[] owners = [KeyboardContext.Modal, KeyboardContext.TextEntry, KeyboardContext.Settings,
            KeyboardContext.Bookmarks, KeyboardContext.BookmarkDraft, KeyboardContext.CommandPalette,
            KeyboardContext.KeyboardHelp, KeyboardContext.Locations, KeyboardContext.WindowAdjustment];
        foreach (KeyboardContext context in owners)
        {
            Assert.IsNotInstanceOfType<MappedKeyboardFocusAction>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F10, context)));
        }
    }

    /// <summary>Proves every canonical focus binding appears in the existing help projection.</summary>
    [TestMethod]
    public void PresentWhenFocusBindingsDeclaredIncludesTheirCanonicalCapsAndLabels()
    {
        KeyboardHelpPresentation help = KeyboardHelpPresenter.Present();
        foreach (KeyboardFocusBinding binding in KeyboardIntentMapper.FocusBindings)
        {
            Assert.IsTrue(help.PrimaryGroups.Concat(help.SecondaryGroups).SelectMany(group => group.Rows).Any(row =>
                row.ActionLabelResourceKey == binding.LabelResourceKey && row.KeyLabelResourceKeys.Contains(binding.Key.LabelResourceKey)));
        }
    }
}
