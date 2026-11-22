using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Commands;
using NeNeCommander.Presentation.WinUI.Help;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;
using Windows.System;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves Ctrl+E uses the canonical translated mapping without leaking into text or repeating.</summary>
[TestClass]
public sealed class OtherPaneKeyboardTests
{
    /// <summary>Proves raw E is only a precursor and either control character form maps once in each normal context.</summary>
    [TestMethod]
    [DataRow('e')]
    [DataRow('\u0005')]
    public void MapWhenControlEArrivesInFileListMapsOnlyCharacter(char character)
    {
        AssertCharacterMapping(KeyboardContext.FileList, character);
    }

    /// <summary>Proves the navigation surface uses the same canonical translated command.</summary>
    [TestMethod]
    [DataRow('e')]
    [DataRow('\u0005')]
    public void MapWhenControlEArrivesInNavigationSurfaceMapsOnlyCharacter(char character)
    {
        AssertCharacterMapping(KeyboardContext.NavigationSurface, character);
    }

    private static void AssertCharacterMapping(KeyboardContext context, char character)
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardInput raw = KeyboardInputTranslator.TranslateKeyData((int)VirtualKey.E, KeyRepeatState.Initial, context, KeyboardModifier.Control);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(raw));

        KeyboardInput translated = Character(character, KeyRepeatState.Initial, context, KeyboardModifier.Control);
        MappedKeyboardIntent mapped = Assert.IsInstanceOfType<MappedKeyboardIntent>(mapper.Map(translated));

        Assert.AreSame(KeyboardKey.E, translated.Key);
        Assert.AreSame(UserIntent.OpenCurrentLocationInOtherPane, mapped.Intent);
        KeyBinding binding = KeyboardIntentMapper.BindingsFor(context).Single(item => item.Intent == mapped.Intent);
        Assert.AreEqual("KeyLabelCtrlE", binding.KeyLabelResourceKey);
    }

    /// <summary>Proves other owners never emit the opposite command, while plain e remains text or Bookmark editing.</summary>
    [TestMethod]
    public void MapWhenOtherContextOwnsControlENeverEmitsOppositeCommand()
    {
        foreach (KeyboardContext context in new[]
        {
            KeyboardContext.TextEntry, KeyboardContext.AddressEntry, KeyboardContext.Modal,
            KeyboardContext.CommandPalette, KeyboardContext.Settings, KeyboardContext.Bookmarks,
            KeyboardContext.BookmarkSearch, KeyboardContext.BookmarkDraft, KeyboardContext.Locations,
            KeyboardContext.WindowAdjustment, KeyboardContext.KeyboardHelp,
        })
        {
            KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
            KeyboardMappingOutcome outcome = mapper.Map(Character('\u0005', KeyRepeatState.Initial, context, KeyboardModifier.Control));
            Assert.IsFalse(outcome is MappedKeyboardIntent mapped && mapped.Intent == UserIntent.OpenCurrentLocationInOtherPane);
            Assert.IsFalse(KeyboardIntentMapper.BindingsFor(context).Any(binding => binding.Intent == UserIntent.OpenCurrentLocationInOtherPane));
        }
        KeyboardIntentMapper plain = KeyboardMappingTestContext.CreateMapper();
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(plain.Map(Character('e', KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.None)));
        _ = Assert.IsInstanceOfType<MappedBookmarkManagerAction>(plain.Map(Character('e', KeyRepeatState.Initial, KeyboardContext.Bookmarks, KeyboardModifier.None)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(plain.Map(Character('e', KeyRepeatState.Initial, KeyboardContext.TextEntry, KeyboardModifier.None)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(plain.Map(Character('\u0005', KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.None)));
    }

    /// <summary>Proves a held Ctrl+E stays consumed across owners until a fresh identified E press.</summary>
    [TestMethod]
    public void MapWhenControlEIsHeldKeepsSingleShotAcrossContextChanges()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        _ = Assert.IsInstanceOfType<MappedKeyboardIntent>(mapper.Map(Character('\u0005', KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.Control)));
        _ = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.J));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardInputTranslator.TranslateKeyData(
            (int)VirtualKey.E, KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.Control)));
        foreach (KeyboardContext context in new[] { KeyboardContext.FileList, KeyboardContext.NavigationSurface, KeyboardContext.Bookmarks, KeyboardContext.TextEntry })
        {
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(Character('\u0005', KeyRepeatState.Repeated, context, KeyboardModifier.Control)));
        }
        MappedKeyboardIntent fresh = Assert.IsInstanceOfType<MappedKeyboardIntent>(mapper.Map(Character('\u0005', KeyRepeatState.Initial, KeyboardContext.NavigationSurface, KeyboardModifier.Control)));
        Assert.AreSame(UserIntent.OpenCurrentLocationInOtherPane, fresh.Intent);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Character('e', KeyRepeatState.Initial, KeyboardContext.TextEntry, KeyboardModifier.None)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Character('e', KeyRepeatState.Repeated, KeyboardContext.TextEntry, KeyboardModifier.None)));
    }

    /// <summary>Proves a repeated chord without an observed first press is not admitted.</summary>
    [TestMethod]
    public void MapWhenOnlyRepeatedControlEArrivesDoesNotDispatch()
    {
        KeyboardMappingOutcome outcome = KeyboardMappingTestContext.CreateMapper().Map(Character('\u0005', KeyRepeatState.Repeated, KeyboardContext.FileList, KeyboardModifier.Control));
        Assert.IsFalse(outcome is MappedKeyboardIntent);
    }

    /// <summary>Proves F1 and shown footer reuse the same canonical label and shortcut while hidden footer remains F1 only.</summary>
    [TestMethod]
    public void PresentWhenOppositeCommandIsDeclaredUsesCanonicalHelpAndFooterLabels()
    {
        string label = CommandLabelCatalog.LabelFor(UserIntent.OpenCurrentLocationInOtherPane).ResourceKey;
        Assert.AreEqual("IntentLabelOpenCurrentLocationInOtherPane", label);
        KeyHint hint = KeyHintPresenter.Present(KeyboardContext.FileList, KeyHintVisibility.Shown).Single(item => item.IntentLabelResourceKey == label);
        Assert.AreEqual("KeyLabelCtrlE", hint.KeyLabelResourceKey);
        KeyboardHelpRow row = KeyboardHelpPresenter.Present().PrimaryGroups.SelectMany(group => group.Rows).Single(item => item.ActionLabelResourceKey == label);
        Assert.HasCount(1, row.KeyLabelResourceKeys);
        Assert.AreEqual("KeyLabelCtrlE", row.KeyLabelResourceKeys[0]);
        Assert.HasCount(1, KeyHintPresenter.Present(KeyboardContext.FileList, KeyHintVisibility.Hidden));
    }

    private static KeyboardInput Character(char character, KeyRepeatState repeat, KeyboardContext context, KeyboardModifier modifier)
    {
        return KeyboardInputTranslator.TranslateCharacterData(character, repeat, context, modifier);
    }
}
