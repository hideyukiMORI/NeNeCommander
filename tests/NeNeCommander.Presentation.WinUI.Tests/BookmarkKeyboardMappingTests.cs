using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>
/// Proves the bookmark manager's canonical key table (ADR-0080): the browse context owns movement,
/// region switching, activation, search, the three commands and closing; the search context owns
/// only the keys that return to the list; held commands act once and undeclared keys pass through.
/// </summary>
[TestClass]
public sealed class BookmarkKeyboardMappingTests
{
    /// <summary>Proves every browse key maps to its declared action.</summary>
    [TestMethod]
    public void MapWhenBookmarksOwnsInputMapsEveryDeclaredKey()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardContext browse = KeyboardContext.Bookmarks;

        Assert.AreSame(BookmarkManagerKeyAction.MoveNext, Mapped(mapper, KeyboardKey.J, browse));
        Assert.AreSame(BookmarkManagerKeyAction.MoveNext, Mapped(mapper, KeyboardKey.Down, browse));
        Assert.AreSame(BookmarkManagerKeyAction.MovePrevious, Mapped(mapper, KeyboardKey.K, browse));
        Assert.AreSame(BookmarkManagerKeyAction.MovePrevious, Mapped(mapper, KeyboardKey.Up, browse));
        Assert.AreSame(BookmarkManagerKeyAction.Activate, Mapped(mapper, KeyboardKey.Enter, browse));
        Assert.AreSame(BookmarkManagerKeyAction.FocusSearch, Mapped(mapper, KeyboardKey.Slash, browse));
        Assert.AreSame(BookmarkManagerKeyAction.SwitchRegion, Mapped(mapper, KeyboardKey.Tab, browse));
        Assert.AreSame(BookmarkManagerKeyAction.Add, Mapped(mapper, KeyboardKey.A, browse));
        Assert.AreSame(BookmarkManagerKeyAction.Edit, Mapped(mapper, KeyboardKey.E, browse));
        Assert.AreSame(BookmarkManagerKeyAction.Delete, Mapped(mapper, KeyboardKey.D, browse));
        Assert.AreSame(BookmarkManagerKeyAction.Close, Mapped(mapper, KeyboardKey.Escape, browse));
    }

    /// <summary>Proves held movement repeats while every other held key acts once.</summary>
    [TestMethod]
    public void MapWhenKeyRepeatsRepeatsOnlyMovement()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardContext browse = KeyboardContext.Bookmarks;

        Assert.AreSame(BookmarkManagerKeyAction.MoveNext, RepeatedAction(mapper, KeyboardKey.J, browse));
        Assert.AreSame(BookmarkManagerKeyAction.MovePrevious, RepeatedAction(mapper, KeyboardKey.Up, browse));
        foreach (KeyboardKey key in new[] { KeyboardKey.Enter, KeyboardKey.A, KeyboardKey.E, KeyboardKey.D, KeyboardKey.Tab, KeyboardKey.Slash, KeyboardKey.Escape })
        {
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(
                mapper.Map(KeyboardMappingTestContext.Repeated(key, browse)),
                key.LabelResourceKey);
        }
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(KeyboardMappingTestContext.Repeated(KeyboardKey.Escape, KeyboardContext.BookmarkSearch)));
    }

    /// <summary>Proves undeclared keys and declared keys under a modifier pass through to the controls.</summary>
    [TestMethod]
    public void MapWhenKeyIsUndeclaredPassesThrough()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardContext browse = KeyboardContext.Bookmarks;

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Other, browse)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Space, browse)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F8, browse)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.D, KeyboardModifier.Control, browse)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.B, KeyboardModifier.Control, browse)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Tab, KeyboardModifier.Alt, browse)));
    }

    /// <summary>Proves the search field keeps its text keys and returns to the list on Escape, Down or Enter.</summary>
    [TestMethod]
    public void MapWhenSearchOwnsInputLeavesOnlyOnItsThreeKeys()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardContext search = KeyboardContext.BookmarkSearch;

        Assert.AreSame(BookmarkManagerKeyAction.LeaveSearch, Mapped(mapper, KeyboardKey.Escape, search));
        Assert.AreSame(BookmarkManagerKeyAction.LeaveSearch, Mapped(mapper, KeyboardKey.Down, search));
        Assert.AreSame(BookmarkManagerKeyAction.LeaveSearch, Mapped(mapper, KeyboardKey.Enter, search));
        foreach (KeyboardKey key in new[] { KeyboardKey.J, KeyboardKey.K, KeyboardKey.A, KeyboardKey.E, KeyboardKey.D, KeyboardKey.Slash, KeyboardKey.Up, KeyboardKey.Tab, KeyboardKey.Other })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
                mapper.Map(KeyboardMappingTestContext.Input(key, search)),
                key.LabelResourceKey);
        }
    }

    /// <summary>Proves each context declares each key once and every declaration maps to its own action.</summary>
    [TestMethod]
    public void BookmarkManagerBindingsWhenMappedEmitTheirDeclaredActionOnce()
    {
        IReadOnlyList<BookmarkManagerKeyBinding> bindings = KeyboardIntentMapper.BookmarkManagerBindings;
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        HashSet<string> keystrokes = [];

        Assert.HasCount(14, bindings);
        Assert.AreEqual(11, bindings.Count(binding => binding.Context == KeyboardContext.Bookmarks));
        Assert.AreEqual(3, bindings.Count(binding => binding.Context == KeyboardContext.BookmarkSearch));
        foreach (BookmarkManagerKeyBinding binding in bindings)
        {
            Assert.IsTrue(keystrokes.Add(binding.Context.GetType().Name + " " + binding.KeyLabelResourceKey));
            Assert.AreSame(binding.Action, Mapped(mapper, binding.Key, binding.Context));
            Assert.AreEqual(binding.Key.LabelResourceKey, binding.KeyLabelResourceKey);
        }
        Assert.IsEmpty(KeyboardIntentMapper.BindingsFor(KeyboardContext.Bookmarks));
        Assert.IsEmpty(KeyboardIntentMapper.BindingsFor(KeyboardContext.BookmarkSearch));
    }

    /// <summary>Proves the new letters and the slash are produced characters with their own key caps.</summary>
    [TestMethod]
    public void TranslateCharacterDataWhenLetterOrSlashArrivesIdentifiesTheKey()
    {
        Assert.AreSame(KeyboardKey.A, Translated('a'));
        Assert.AreSame(KeyboardKey.E, Translated('e'));
        Assert.AreSame(KeyboardKey.Slash, Translated('/'));
        Assert.AreSame(KeyboardKey.Other, Translated('A'));
        Assert.AreEqual("KeyLabelA", KeyboardKey.A.LabelResourceKey);
        Assert.AreEqual("KeyLabelE", KeyboardKey.E.LabelResourceKey);
        Assert.AreEqual("KeyLabelSlash", KeyboardKey.Slash.LabelResourceKey);
    }

    /// <summary>Proves the new letters stay unmapped in the file list, so its keys and chord are unchanged.</summary>
    [TestMethod]
    public void MapWhenNewLettersArriveInFileListPassesThrough()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        foreach (KeyboardKey key in new[] { KeyboardKey.A, KeyboardKey.E, KeyboardKey.Slash })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(key)));
        }
    }

    private static KeyboardKey Translated(char character)
    {
        return KeyboardInputTranslator.TranslateCharacterData(
            character,
            KeyRepeatState.Initial,
            KeyboardContext.Bookmarks,
            KeyboardModifier.None).Key;
    }

    private static BookmarkManagerKeyAction Mapped(KeyboardIntentMapper mapper, KeyboardKey key, KeyboardContext context)
    {
        return Assert.IsInstanceOfType<MappedBookmarkManagerAction>(
            mapper.Map(KeyboardMappingTestContext.Input(key, context))).Action;
    }

    private static BookmarkManagerKeyAction RepeatedAction(KeyboardIntentMapper mapper, KeyboardKey key, KeyboardContext context)
    {
        return Assert.IsInstanceOfType<MappedBookmarkManagerAction>(
            mapper.Map(KeyboardMappingTestContext.Repeated(key, context))).Action;
    }
}
