using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Bookmarks;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves queued bookmark focus respects later ownership changes without a native window.</summary>
[TestClass]
public sealed class BookmarkFocusRestorationTests
{
    /// <summary>Proves automatic focus during layout does not discard a requested state transition.</summary>
    [TestMethod]
    public void RequestWhenLayoutTemporarilyFocusesSearchKeepsTheRequestedTarget()
    {
        List<Action> queued = [];
        BookmarkFocusRestoration restoration = new(queued.Add);
        int focusCount = 0;
        restoration.Request(() => focusCount++);
        Assert.IsTrue(restoration.IsPending);

        restoration.CancelRestoration();
        queued[0]();

        Assert.AreEqual(1, focusCount);
        Assert.IsFalse(restoration.IsPending);
    }

    /// <summary>Proves native focus changes still cancel a passive row-regeneration restoration.</summary>
    [TestMethod]
    public void RestoreWhenNativeFocusChangesDoesNotTakeFocusBack()
    {
        List<Action> queued = [];
        BookmarkFocusRestoration restoration = new(queued.Add);
        int focusCount = 0;
        restoration.Restore(() => focusCount++);

        restoration.CancelRestoration();
        queued[0]();

        Assert.AreEqual(0, focusCount);
    }

    /// <summary>Proves later native editing cancels a request retained through incidental layout focus.</summary>
    [TestMethod]
    public void RequestWhenCancelledBeforeDispatchDoesNotRestoreFocus()
    {
        List<Action> queued = [];
        BookmarkFocusRestoration restoration = new(queued.Add);
        int focusCount = 0;
        restoration.Request(() => focusCount++);
        restoration.CancelRestoration();
        Assert.IsTrue(restoration.IsPending);
        restoration.Cancel();
        Assert.IsFalse(restoration.IsPending);

        queued[0]();

        Assert.AreEqual(0, focusCount);
    }

    /// <summary>Proves an old region callback cannot run or discard the newer region request.</summary>
    [TestMethod]
    public void RequestWhenAnotherRegionSupersedesItRestoresOnlyTheLatestRegion()
    {
        List<Action> queued = [];
        BookmarkFocusRestoration restoration = new(queued.Add);
        int listFocusCount = 0;
        int categoryFocusCount = 0;
        restoration.Request(() => listFocusCount++);
        restoration.Request(() => categoryFocusCount++);

        queued[0]();
        queued[1]();

        Assert.AreEqual(0, listFocusCount);
        Assert.AreEqual(1, categoryFocusCount);
    }

    /// <summary>Proves an old callback arriving last cannot undo the latest focus.</summary>
    [TestMethod]
    public void RequestWhenOldCallbackArrivesAfterTheNewOneKeepsTheLatestFocus()
    {
        List<Action> queued = [];
        BookmarkFocusRestoration restoration = new(queued.Add);
        BookmarkManagerRegion? focused = null;
        restoration.Request(() => focused = BookmarkManagerRegion.Bookmarks);
        restoration.Request(() => focused = BookmarkManagerRegion.Categories);

        queued[1]();
        queued[0]();

        Assert.AreSame(BookmarkManagerRegion.Categories, focused);
    }

    /// <summary>Proves a reopened list has a new request even when its native action is reused.</summary>
    [TestMethod]
    public void RequestWhenCancelledAndRequestedAgainRejectsTheOldRequestIdentity()
    {
        List<Action> queued = [];
        BookmarkFocusRestoration restoration = new(queued.Add);
        int focusCount = 0;
        void Focus()
        {
            focusCount++;
        }
        Action focus = Focus;
        restoration.Request(focus);
        restoration.Cancel();
        restoration.Request(focus);

        queued[0]();
        Assert.AreEqual(0, focusCount);
        queued[1]();

        Assert.AreEqual(1, focusCount);
    }

    /// <summary>Proves row regeneration cannot replace a pending explicit region change.</summary>
    [TestMethod]
    public void RestoreWhenRegionChangeIsPendingKeepsTheRequestedRegion()
    {
        List<Action> queued = [];
        BookmarkFocusRestoration restoration = new(queued.Add);
        BookmarkManagerRegion? focused = null;
        restoration.Request(() => focused = BookmarkManagerRegion.Categories);

        restoration.Restore(() => focused = BookmarkManagerRegion.Bookmarks);
        Assert.HasCount(1, queued);
        queued[0]();

        Assert.AreSame(BookmarkManagerRegion.Categories, focused);
    }

    /// <summary>Proves an unopposed row regeneration still restores native focus.</summary>
    [TestMethod]
    public void RestoreWhenNoRequestIsPendingAppliesTheFocusAfterDispatch()
    {
        List<Action> queued = [];
        BookmarkFocusRestoration restoration = new(queued.Add);
        int focusCount = 0;

        restoration.Restore(() => focusCount++);
        Assert.AreEqual(0, focusCount);
        queued[0]();

        Assert.AreEqual(1, focusCount);
    }
}
