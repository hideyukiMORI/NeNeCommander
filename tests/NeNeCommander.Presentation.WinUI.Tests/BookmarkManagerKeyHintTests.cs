using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>
/// Proves the bookmark manager's key band (ADR-0080): every cap comes from the canonical bookmark
/// table, the wording follows the focused region, and only the movement hint cannot be pressed.
/// </summary>
[TestClass]
public sealed class BookmarkManagerKeyHintTests
{
    private static readonly string[] ListLabels =
    [
        "BookmarkHintMove",
        "BookmarkHintOpen",
        "BookmarkHintSearch",
        "BookmarkHintSwitch",
        "BookmarkHintAddBookmark",
        "BookmarkHintEditBookmark",
        "BookmarkHintDeleteBookmark",
        "BookmarkHintClose",
    ];
    private static readonly string[] CategoryLabels =
    [
        "BookmarkHintMove",
        "BookmarkHintToList",
        "BookmarkHintSearch",
        "BookmarkHintSwitch",
        "BookmarkHintAddCategory",
        "BookmarkHintRenameCategory",
        "BookmarkHintDeleteCategory",
        "BookmarkHintClose",
    ];
    private static readonly string[] Caps =
    [
        "KeyLabelJ",
        "KeyLabelK",
        "KeyLabelEnter",
        "KeyLabelSlash",
        "KeyLabelTab",
        "KeyLabelA",
        "KeyLabelE",
        "KeyLabelD",
        "KeyLabelEscape",
    ];

    /// <summary>Proves the list and the column show the same keys in one order with their own wording.</summary>
    [TestMethod]
    public void PresentWhenRegionVariesKeepsKeysAndChangesWording()
    {
        IReadOnlyList<BookmarkManagerKeyHint> list = BookmarkManagerKeyHintPresenter.Present(BookmarkManagerRegion.Bookmarks);
        IReadOnlyList<BookmarkManagerKeyHint> column = BookmarkManagerKeyHintPresenter.Present(BookmarkManagerRegion.Categories);

        CollectionAssert.AreEqual(ListLabels, list.Select(hint => hint.LabelResourceKey).ToArray());
        CollectionAssert.AreEqual(CategoryLabels, column.Select(hint => hint.LabelResourceKey).ToArray());
        CollectionAssert.AreEqual(Caps, list.SelectMany(hint => hint.KeyLabelResourceKeys).ToArray());
        CollectionAssert.AreEqual(Caps, column.SelectMany(hint => hint.KeyLabelResourceKeys).ToArray());
        CollectionAssert.AreEqual(
            list.Select(hint => hint.Action).ToArray(),
            column.Select(hint => hint.Action).ToArray());
        Assert.HasCount(list.Count, list.Select(hint => hint.AutomationId).Distinct());
    }

    /// <summary>Proves every shown cap is a key the canonical table maps to that hint's action.</summary>
    [TestMethod]
    public void PresentWhenProjectedShowsOnlyDeclaredKeysOfEachAction()
    {
        IReadOnlyList<BookmarkManagerKeyBinding> bindings = KeyboardIntentMapper.BookmarkManagerBindings;

        foreach (BookmarkManagerKeyHint hint in BookmarkManagerKeyHintPresenter.Present(BookmarkManagerRegion.Bookmarks).Skip(1))
        {
            string cap = hint.KeyLabelResourceKeys.Single();
            Assert.IsTrue(bindings.Any(binding =>
                binding.Context == KeyboardContext.Bookmarks &&
                binding.Action == hint.Action &&
                binding.KeyLabelResourceKey == cap), hint.LabelResourceKey);
            Assert.IsTrue(hint.IsInvocable, hint.LabelResourceKey);
        }
        BookmarkManagerKeyHint move = BookmarkManagerKeyHintPresenter.Present(BookmarkManagerRegion.Bookmarks)[0];
        Assert.IsFalse(move.IsInvocable);
        Assert.AreSame(BookmarkManagerKeyAction.MoveNext, move.Action);
        Assert.AreEqual("BookmarkHintMove", move.AutomationId);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => BookmarkManagerKeyHintPresenter.Present(null!));
    }
}
