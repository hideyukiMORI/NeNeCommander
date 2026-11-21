using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves one bookmark command press cannot repeat into the next input owner.</summary>
[TestClass]
public sealed class BookmarkKeyboardRepeatTests
{
    /// <summary>Proves a navigation Enter cannot open a file after the manager has closed.</summary>
    [TestMethod]
    public void MapWhenBookmarkEnterChangesOwnerConsumesRepeatUntilNewInitialEnter()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        _ = Assert.IsInstanceOfType<MappedBookmarkManagerAction>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardContext.Bookmarks)));
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.J), UserIntent.MoveNext);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Other)));

        foreach (KeyboardContext context in new[]
        {
            KeyboardContext.FileList,
            KeyboardContext.AddressEntry,
            KeyboardContext.Modal,
            KeyboardContext.BookmarkSearch,
        })
        {
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(
                mapper.Map(KeyboardMappingTestContext.Repeated(KeyboardKey.Enter, context)));
        }

        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Enter), UserIntent.OpenFocused);
        KeyboardMappingTestContext.AssertMaps(
            mapper,
            KeyboardMappingTestContext.Repeated(KeyboardKey.Enter, KeyboardContext.FileList),
            UserIntent.OpenFocused);
    }

    /// <summary>Proves holding an opening character does not replace the search or draft text.</summary>
    [TestMethod]
    [DataRow('/')]
    [DataRow('a')]
    [DataRow('e')]
    public void MapWhenCharacterOpensEditorConsumesItsRepeatButAllowsTheNextPress(char character)
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardContext editor = character == '/' ? KeyboardContext.BookmarkSearch : KeyboardContext.Modal;
        _ = Assert.IsInstanceOfType<MappedBookmarkManagerAction>(
            mapper.Map(Character(character, KeyRepeatState.Initial, KeyboardContext.Bookmarks)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardInputTranslator.TranslateKeyData(
            (int)VirtualKey.A,
            KeyRepeatState.Repeated,
            editor,
            KeyboardModifier.None)));

        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(Character(character, KeyRepeatState.Repeated, editor)));

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(Character(character, KeyRepeatState.Initial, editor)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(Character(character, KeyRepeatState.Repeated, editor)));
    }

    /// <summary>Proves another command and raw events cannot release a still-held opening key.</summary>
    [TestMethod]
    public void MapWhenSingleShotPressesOverlapKeepsEachPressUntilItsNextInitialEvent()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        _ = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Slash, KeyboardContext.Bookmarks));
        _ = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardContext.BookmarkSearch));
        _ = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Other, KeyboardContext.Bookmarks));

        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(KeyboardMappingTestContext.Repeated(KeyboardKey.Slash, KeyboardContext.Bookmarks)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(KeyboardMappingTestContext.Repeated(KeyboardKey.Enter, KeyboardContext.FileList)));

        _ = Assert.IsInstanceOfType<MappedBookmarkManagerAction>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Slash, KeyboardContext.Bookmarks)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(KeyboardMappingTestContext.Repeated(KeyboardKey.Enter, KeyboardContext.FileList)));
    }

    /// <summary>Proves each table-declared single-shot action keeps its press across an owner change.</summary>
    [TestMethod]
    public void MapWhenSingleShotBookmarkBindingChangesOwnerConsumesItsRepeat()
    {
        foreach (BookmarkManagerKeyBinding binding in KeyboardIntentMapper.BookmarkManagerBindings)
        {
            if (binding.Action.Repeats)
            {
                continue;
            }
            KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
            _ = Assert.IsInstanceOfType<MappedBookmarkManagerAction>(
                mapper.Map(KeyboardMappingTestContext.Input(binding.Key, binding.Modifier, binding.Context)));

            _ = Assert.IsInstanceOfType<KeyboardConsumed>(
                mapper.Map(KeyboardMappingTestContext.Repeated(binding.Key, KeyboardContext.TextEntry)),
                binding.KeyLabelResourceKey);
        }
    }

    /// <summary>Proves ordinary held text and movement do not acquire a single-shot guard.</summary>
    [TestMethod]
    public void MapWhenMovementRepeatsOrTextIsTypedPreservesItsExistingOwner()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        foreach (KeyboardKey key in new[] { KeyboardKey.J, KeyboardKey.K })
        {
            MappedBookmarkManagerAction initial = Assert.IsInstanceOfType<MappedBookmarkManagerAction>(
                mapper.Map(KeyboardMappingTestContext.Input(key, KeyboardContext.Bookmarks)));
            MappedBookmarkManagerAction repeated = Assert.IsInstanceOfType<MappedBookmarkManagerAction>(
                mapper.Map(KeyboardMappingTestContext.Repeated(key, KeyboardContext.Bookmarks)));
            Assert.AreSame(initial.Action, repeated.Action);
        }
        foreach (char character in new[] { '/', 'a', 'e', 'j', 'k', '\u3042' })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
                mapper.Map(Character(character, KeyRepeatState.Initial, KeyboardContext.BookmarkSearch)));
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
                mapper.Map(Character(character, KeyRepeatState.Repeated, KeyboardContext.BookmarkSearch)));
        }
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Repeated(KeyboardKey.Other, KeyboardContext.Bookmarks)));
    }

    /// <summary>Proves a retained letter press does not reinterpret an undeclared key as a chord cancellation.</summary>
    [TestMethod]
    public void MapWhenGuardedLetterRepeatsInFileListPreservesThePendingChord()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        _ = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.A, KeyboardContext.Bookmarks));
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));

        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(KeyboardMappingTestContext.Repeated(KeyboardKey.A, KeyboardContext.FileList)));

        KeyboardMappingTestContext.AssertMaps(
            mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.LowerG),
            UserIntent.FocusFirst);
    }

    private static KeyboardInput Character(char character, KeyRepeatState repeat, KeyboardContext context)
    {
        return KeyboardInputTranslator.TranslateCharacterData(character, repeat, context, KeyboardModifier.None);
    }
}
