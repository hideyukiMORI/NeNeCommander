using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves draft saving and native composition cannot activate a later owner.</summary>
[TestClass]
public sealed class BookmarkDraftKeyboardTests
{
    private static readonly string[] SearchCaps = ["KeyLabelEscape", "KeyLabelDown", "KeyLabelEnter"];

    /// <summary>Proves only Ctrl+Enter saves and the action follows the existing bookmark submission.</summary>
    [TestMethod]
    public void DraftSaveUsesControlEnterAndPreservesNativeEditingKeys()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardContext draft = KeyboardContext.BookmarkDraft;
        foreach (KeyboardKey key in new[] { KeyboardKey.Enter, KeyboardKey.Up, KeyboardKey.Down, KeyboardKey.Tab, KeyboardKey.A })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(key, draft)));
        }
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(
            KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardModifier.Alt, draft)));

        MappedBookmarkManagerAction mapped = Assert.IsInstanceOfType<MappedBookmarkManagerAction>(mapper.Map(
            KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardModifier.Control, draft)));

        Assert.AreSame(BookmarkManagerKeyAction.Save, mapped.Action);
        Assert.AreEqual(UserIntent.ManageBookmarks(BookmarkEditorAction.Save),
            BookmarkManagerKeyboard.Resolve(BookmarkManagerRegion.Categories, mapped.Action, null, null).Intent);
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
            KeyboardMappingTestContext.Repeated(KeyboardKey.Enter, KeyboardContext.Bookmarks)));
    }

    /// <summary>Proves native Save/Cancel Enter cannot resubmit a rejected draft or reach a later owner.</summary>
    [TestMethod]
    public void NativeDraftEnterActsOnceEvenWhenTheDraftIsRejectedOrOwnershipChanges()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
            KeyboardMappingTestContext.Repeated(KeyboardKey.Enter, KeyboardContext.BookmarkDraft)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(
            KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardContext.BookmarkDraft)));

        foreach (KeyboardContext context in new[] { KeyboardContext.BookmarkDraft, KeyboardContext.Bookmarks, KeyboardContext.FileList })
        {
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardMappingTestContext.Repeated(KeyboardKey.Enter, context)));
        }
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(
            KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardContext.BookmarkDraft)));
    }

    /// <summary>Proves a native composition or popup press cannot repeat into save, cancel or activation.</summary>
    [TestMethod]
    public void NativeEnterAndEscapeKeepTheirPressUntilTheNextInitialEvent()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        foreach (KeyboardKey key in new[] { KeyboardKey.Enter, KeyboardKey.Escape })
        {
            mapper.ObserveNativeInput(KeyboardMappingTestContext.Input(key, KeyboardContext.BookmarkDraft));
            foreach (KeyboardContext context in new[] { KeyboardContext.BookmarkDraft, KeyboardContext.Bookmarks, KeyboardContext.Modal })
            {
                _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardMappingTestContext.Repeated(key, context)));
            }
        }
        _ = Assert.IsInstanceOfType<MappedBookmarkManagerAction>(mapper.Map(
            KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardModifier.Control, KeyboardContext.BookmarkDraft)));
        _ = Assert.IsInstanceOfType<MappedBookmarkManagerAction>(mapper.Map(
            KeyboardMappingTestContext.Input(KeyboardKey.Escape, KeyboardContext.BookmarkDraft)));
    }

    /// <summary>Proves the search hint tells the truth and does not advertise navigation or editing.</summary>
    [TestMethod]
    public void SearchHintOnlyReturnsToTheList()
    {
        BookmarkManagerKeyHint hint = BookmarkManagerKeyHintPresenter.PresentSearch()[0];
        CollectionAssert.AreEqual(SearchCaps,
            new System.Collections.Generic.List<string>(hint.KeyLabelResourceKeys));
        Assert.AreEqual("BookmarkHintToList", hint.LabelResourceKey);
        Assert.AreSame(BookmarkManagerKeyAction.LeaveSearch, hint.Action);
        Assert.AreEqual("KeyLabelCtrlEnter", BookmarkManagerKeyHintPresenter.SaveKeyLabelResourceKey);
    }
}
