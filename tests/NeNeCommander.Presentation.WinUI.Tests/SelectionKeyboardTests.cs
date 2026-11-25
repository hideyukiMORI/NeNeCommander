using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves selection chords use one context-aware mapper and physical-press owner.</summary>
[TestClass]
public sealed class SelectionKeyboardTests
{
    /// <summary>Proves all eight declared keys dispatch only their canonical selection intents.</summary>
    [TestMethod]
    public void MapWhenSelectionKeyIsDeclaredUsesCanonicalIntentAndHint()
    {
        KeyboardKey[] keys = [KeyboardKey.A, KeyboardKey.Star, KeyboardKey.Down, KeyboardKey.Up,
            KeyboardKey.PageDown, KeyboardKey.PageUp, KeyboardKey.Home, KeyboardKey.End];
        UserIntent[] intents = [UserIntent.SelectAll, UserIntent.InvertSelection, UserIntent.SelectRangeNext,
            UserIntent.SelectRangePrevious, UserIntent.SelectRangeHalfPageDown, UserIntent.SelectRangeHalfPageUp,
            UserIntent.SelectRangeFirst, UserIntent.SelectRangeLast];
        string[] caps = ["KeyLabelCtrlA", "KeyLabelStar", "KeyLabelShiftDown", "KeyLabelShiftUp",
            "KeyLabelShiftPageDown", "KeyLabelShiftPageUp", "KeyLabelShiftHome", "KeyLabelShiftEnd"];

        for (int index = 0; index < keys.Length; index++)
        {
            KeyboardModifier modifier = index == 0 ? KeyboardModifier.Control : index == 1 ? KeyboardModifier.None : KeyboardModifier.Shift;
            KeyboardMappingTestContext.AssertMaps(KeyboardMappingTestContext.CreateMapper(),
                KeyboardMappingTestContext.Input(keys[index], modifier), intents[index]);
            KeyBinding binding = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList).Single(item => item.Intent == intents[index]);
            Assert.AreSame(keys[index], binding.Key);
            Assert.AreSame(modifier, binding.Modifier);
            Assert.AreEqual(caps[index], binding.KeyLabelResourceKey);
        }
    }

    /// <summary>Proves ranges repeat while all and inversion execute at most once per physical press.</summary>
    [TestMethod]
    public void MapWhenSelectionKeyRepeatsRetainsOneShotBulkAndRepeatingRanges()
    {
        foreach ((KeyboardKey key, KeyboardModifier modifier, UserIntent intent) in new[]
        {
            (KeyboardKey.A, KeyboardModifier.Control, UserIntent.SelectAll),
            (KeyboardKey.Star, KeyboardModifier.None, UserIntent.InvertSelection),
        })
        {
            KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
            KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(key, modifier), intent);
            foreach (KeyboardContext context in new[] { KeyboardContext.FileList, KeyboardContext.TextEntry,
                KeyboardContext.ContextMenu, KeyboardContext.Modal, KeyboardContext.BookmarkDraft })
            {
                _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
                    KeyboardInput.Create(key, modifier, KeyRepeatState.Repeated, context)));
            }
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(KeyboardMappingTestContext.CreateMapper().Map(
                KeyboardInput.Create(key, modifier, KeyRepeatState.Repeated, KeyboardContext.FileList)));
            KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(key, modifier), intent);
        }
        KeyboardMappingTestContext.AssertMaps(KeyboardMappingTestContext.CreateMapper(),
            KeyboardInput.Create(KeyboardKey.Down, KeyboardModifier.Shift, KeyRepeatState.Repeated, KeyboardContext.FileList),
            UserIntent.SelectRangeNext);
    }

    /// <summary>Proves native editors keep select-all and produced symbols.</summary>
    [TestMethod]
    public void MapWhenTextEditorOwnsInputPassesSelectionChordsThrough()
    {
        foreach (KeyboardContext context in new[] { KeyboardContext.TextEntry, KeyboardContext.AddressEntry,
            KeyboardContext.BookmarkSearch, KeyboardContext.BookmarkDraft })
        {
            KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.A, KeyboardModifier.Control, context)));
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Star, KeyboardModifier.None, context)));

        }
    }

    /// <summary>Proves undeclared modifiers and plain Home/End never select files.</summary>
    [TestMethod]
    public void MapWhenModifierOrKeyIsUndeclaredDoesNotSelect()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        foreach (KeyboardKey key in new[] { KeyboardKey.A, KeyboardKey.Star, KeyboardKey.Down, KeyboardKey.Up,
            KeyboardKey.PageDown, KeyboardKey.PageUp, KeyboardKey.Home, KeyboardKey.End })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(key, KeyboardModifier.Other)));
        }
        foreach (KeyboardKey key in new[] { KeyboardKey.A, KeyboardKey.Home, KeyboardKey.End })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(key)));
        }
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Star, KeyboardModifier.Control)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.J, KeyboardModifier.Shift)));
    }

    /// <summary>Proves a declared selection key cancels gg and executes normally.</summary>
    [TestMethod]
    public void MapWhenSelectionKeyFollowsChordPrefixCancelsPrefixAndDispatches()
    {
        foreach ((KeyboardKey key, KeyboardModifier modifier, UserIntent intent) in new[]
        {
            (KeyboardKey.A, KeyboardModifier.Control, UserIntent.SelectAll),
            (KeyboardKey.Star, KeyboardModifier.None, UserIntent.InvertSelection),
            (KeyboardKey.Down, KeyboardModifier.Shift, UserIntent.SelectRangeNext),
        })
        {
            KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
            _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));

            KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(key, modifier), intent);

            _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
        }
    }
}
