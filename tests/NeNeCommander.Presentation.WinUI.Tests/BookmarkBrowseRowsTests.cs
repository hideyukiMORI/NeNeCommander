using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Presentation.WinUI.Bookmarks;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Protects stable bookmark sources, bounded row notifications and complete action captures.</summary>
[TestClass]
public sealed class BookmarkBrowseRowsTests
{
    /// <summary>Proves category movement changes only its two marks, without resetting other rows.</summary>
    [TestMethod]
    public void UpdateWhenCategorySelectionChangesRetainsSourcesAndUnchangedRows()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([work], []);
        BookmarkBrowseRows source = new();
        source.Update(Browse(catalog, BookmarkCategoryFilter.All));
        IReadOnlyList<BookmarkCategoryOption> categories = source.Categories;
        IReadOnlyList<BookmarkRow> rows = source.Rows;
        BookmarkCategoryOption unchanged = categories[1];
        List<NotifyCollectionChangedEventArgs> changes = Observe(categories);

        source.Update(Browse(catalog, BookmarkCategoryFilter.For(work)));

        Assert.AreSame(categories, source.Categories);
        Assert.AreSame(rows, source.Rows);
        Assert.AreSame(unchanged, source.Categories[1]);
        int[] expectedIndexes = [0, 2];
        CollectionAssert.AreEqual(expectedIndexes, changes.Select(change => change.NewStartingIndex).ToArray());
        Assert.IsTrue(changes.All(change => change.Action == NotifyCollectionChangedAction.Replace));
        Assert.IsTrue(source.Categories[2].IsSelected);
        Assert.IsFalse(source.Categories[0].IsSelected);
    }

    /// <summary>Proves equal freshly captured category entries do not churn the native list.</summary>
    [TestMethod]
    public void UpdateWhenSnapshotIsEquivalentKeepsEveryCategoryAndEmitsNothing()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkEntry entry = BookmarkPresenterTestContext.Entry("Repo", "C:\\repo", work, null);
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([work], [entry]);
        BookmarkBrowseRows source = new();
        source.Update(Browse(catalog, BookmarkCategoryFilter.All));
        BookmarkCategoryOption[] categories = [.. source.Categories];
        BookmarkRow row = source.Rows[0];
        List<NotifyCollectionChangedEventArgs> categoryChanges = Observe(source.Categories);
        List<NotifyCollectionChangedEventArgs> rowChanges = Observe(source.Rows);

        source.Update(Browse(catalog, BookmarkCategoryFilter.All));

        Assert.HasCount(0, categoryChanges);
        Assert.HasCount(0, rowChanges);
        for (int index = 0; index < categories.Length; index++)
        {
            Assert.AreSame(categories[index], source.Categories[index]);
        }
        Assert.AreSame(row, source.Rows[0]);
    }

    /// <summary>Proves equal counts cannot hide changed entries used by stale-action rejection.</summary>
    [TestMethod]
    public void UpdateWhenCategoryEntryChangesReplacesCompleteCapturedSelection()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkEntry oldEntry = BookmarkPresenterTestContext.Entry("Repo", "C:\\old", work, null);
        BookmarkEntry newEntry = BookmarkPresenterTestContext.Entry("Repo", "C:\\new", work, null);
        BookmarkBrowseRows source = new();
        source.Update(Browse(BookmarkPresenterTestContext.Catalog([work], [oldEntry]), BookmarkCategoryFilter.All));
        BookmarkCategoryOption previous = source.Categories[2];
        List<NotifyCollectionChangedEventArgs> changes = Observe(source.Categories);

        source.Update(Browse(BookmarkPresenterTestContext.Catalog([work], [newEntry]), BookmarkCategoryFilter.All));

        Assert.AreNotSame(previous, source.Categories[2]);
        Assert.AreEqual(newEntry, source.Categories[2].Selection?.Entries.Single());
        Assert.AreEqual(oldEntry, previous.Selection?.Entries.Single());
        Assert.HasCount(1, changes);
        Assert.AreEqual(NotifyCollectionChangedAction.Replace, changes[0].Action);
        Assert.AreEqual(2, changes[0].NewStartingIndex);
    }

    /// <summary>Proves row selection updates both marks while retaining unaffected row objects.</summary>
    [TestMethod]
    public void UpdateWhenBookmarkSelectionChangesDoesNotResetTheList()
    {
        BookmarkRow first = Row("First", BookmarkOptionSelection.Selected);
        BookmarkRow second = Row("Second", BookmarkOptionSelection.NotSelected);
        BookmarkRow third = Row("Third", BookmarkOptionSelection.NotSelected);
        BookmarkBrowseRows source = new();
        source.Update(new BookmarkBrowsePresentation("", [], [first, second, third], BookmarkEmptyContent.Hidden));
        IReadOnlyList<BookmarkRow> rows = source.Rows;
        List<NotifyCollectionChangedEventArgs> changes = Observe(rows);

        source.Update(new BookmarkBrowsePresentation("", [],
            [Row("First", BookmarkOptionSelection.NotSelected), Row("Second", BookmarkOptionSelection.Selected), third],
            BookmarkEmptyContent.Hidden));

        Assert.AreSame(rows, source.Rows);
        Assert.AreSame(third, source.Rows[2]);
        Assert.IsFalse(source.Rows[0].IsSelected);
        Assert.IsTrue(source.Rows[1].IsSelected);
        Assert.HasCount(2, changes);
        Assert.IsTrue(changes.All(change => change.Action == NotifyCollectionChangedAction.Replace));
    }

    /// <summary>Proves empty results and later additions keep the source and send precise notifications.</summary>
    [TestMethod]
    public void UpdateWhenRowsDisappearAndReturnKeepsSourceWithoutReset()
    {
        BookmarkRow row = Row("Repo", BookmarkOptionSelection.Selected);
        BookmarkBrowseRows source = new();
        source.Update(new BookmarkBrowsePresentation("", [], [row], BookmarkEmptyContent.Hidden));
        IReadOnlyList<BookmarkRow> rows = source.Rows;
        List<NotifyCollectionChangedEventArgs> changes = Observe(rows);

        source.Update(new BookmarkBrowsePresentation("missing", [], [], BookmarkEmptyContent.NoMatches));
        Assert.HasCount(0, rows);
        source.Update(new BookmarkBrowsePresentation("", [], [row], BookmarkEmptyContent.Hidden));

        Assert.AreSame(rows, source.Rows);
        Assert.AreSame(row, rows[0]);
        CollectionAssert.AreEqual(
            new[] { NotifyCollectionChangedAction.Remove, NotifyCollectionChangedAction.Add },
            changes.Select(change => change.Action).ToArray());
    }

    /// <summary>Proves switching regions changes only the two visible marks, not unselected rows.</summary>
    [TestMethod]
    public void UpdateWhenFocusedRegionChangesRetainsUnselectedRows()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkBrowsePresentation categories = Browse(
            BookmarkPresenterTestContext.Catalog([work], []), BookmarkCategoryFilter.For(work));
        BookmarkBrowsePresentation browse = new("", categories.Categories,
            [Row("First", BookmarkOptionSelection.Selected), Row("Second", BookmarkOptionSelection.NotSelected)],
            BookmarkEmptyContent.Hidden);
        BookmarkBrowseRows source = new();
        source.Update(BookmarkBrowseFocusPresenter.Present(browse, BookmarkManagerRegion.Categories));
        BookmarkCategoryOption unselectedCategory = source.Categories[0];
        BookmarkRow unselectedRow = source.Rows[1];
        List<NotifyCollectionChangedEventArgs> categoryChanges = Observe(source.Categories);
        List<NotifyCollectionChangedEventArgs> rowChanges = Observe(source.Rows);

        source.Update(BookmarkBrowseFocusPresenter.Present(browse, BookmarkManagerRegion.Bookmarks));

        Assert.AreSame(unselectedCategory, source.Categories[0]);
        Assert.AreSame(unselectedRow, source.Rows[1]);
        Assert.HasCount(1, categoryChanges);
        Assert.HasCount(1, rowChanges);
        Assert.AreEqual(2, categoryChanges[0].NewStartingIndex);
        Assert.AreEqual(0, rowChanges[0].NewStartingIndex);
        Assert.AreEqual(NeNeCommander.Presentation.WinUI.Panes.PaneRowMark.FocusInPassivePane, source.Categories[2].Mark);
        Assert.AreEqual(NeNeCommander.Presentation.WinUI.Panes.PaneRowMark.FocusInActivePane, source.Rows[0].Mark);
    }

    /// <summary>Proves reordered or rewritten rows expose current selections from the retained sources.</summary>
    [TestMethod]
    public void UpdateWhenSelectedEntriesChangeExposesSelectionsFromCurrentSources()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkEntry first = BookmarkPresenterTestContext.Entry("First", "C:\\first", work, null);
        BookmarkEntry second = BookmarkPresenterTestContext.Entry("Second", "C:\\second", work, null);
        BookmarkBrowseRows source = new();
        BookmarkBrowsePresentation before = Browse(
            BookmarkPresenterTestContext.Catalog([work], [first, second]), BookmarkCategoryFilter.For(work));
        source.Update(new BookmarkBrowsePresentation("", before.Categories,
            [Row("First", BookmarkOptionSelection.Selected), Row("Second", BookmarkOptionSelection.NotSelected)],
            BookmarkEmptyContent.Hidden));
        BookmarkCategoryOption? previousCategory = source.SelectedCategory;
        BookmarkBrowsePresentation after = Browse(
            BookmarkPresenterTestContext.Catalog([work], [second]), BookmarkCategoryFilter.For(work));

        source.Update(new BookmarkBrowsePresentation("", after.Categories,
            [Row("Second", BookmarkOptionSelection.Selected), Row("First", BookmarkOptionSelection.NotSelected)],
            BookmarkEmptyContent.Hidden));

        Assert.AreSame(source.Categories[2], source.SelectedCategory);
        Assert.AreNotSame(previousCategory, source.SelectedCategory);
        Assert.AreEqual(second, source.SelectedCategory?.Selection?.Entries.Single());
        Assert.AreSame(source.Rows[0], source.SelectedRow);
        Assert.AreEqual("Second", source.SelectedRow?.NameText);
        source.Update(new BookmarkBrowsePresentation("", [], [], BookmarkEmptyContent.NoBookmarks));
        Assert.IsNull(source.SelectedCategory);
        Assert.IsNull(source.SelectedRow);
    }

    /// <summary>Proves an invalid input leaves the current source untouched.</summary>
    [TestMethod]
    public void UpdateWhenPresentationIsNullRejectsBeforeChangingRows()
    {
        BookmarkBrowseRows source = new();

        MethodInfo update = typeof(BookmarkBrowseRows).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new AssertFailedException("The row source update boundary was not found.");
        TargetInvocationException invocation = Assert.ThrowsExactly<TargetInvocationException>(() => update.Invoke(source, [null]));
        ArgumentNullException failure = Assert.IsInstanceOfType<ArgumentNullException>(invocation.InnerException);

        Assert.AreEqual("browse", failure.ParamName);
        Assert.HasCount(0, source.Categories);
        Assert.HasCount(0, source.Rows);
    }

    private static BookmarkBrowsePresentation Browse(BookmarkCatalog catalog, BookmarkCategoryFilter filter)
    {
        return BookmarkBrowseFocusPresenter.Present(
            BookmarkPresenterTestContext.Present(catalog, BookmarkPresenterTestContext.Browsing("", filter)).Browse,
            BookmarkManagerRegion.Categories);
    }

    private static BookmarkRow Row(string name, BookmarkOptionSelection selected)
    {
        BookmarkEntry entry = BookmarkPresenterTestContext.Entry(name, "C:\\" + name, null, null);
        return new BookmarkRow(new BookmarkSelection(entry), "Uncategorized", "KeyLabelUnmapped", selected);
    }

    private static List<NotifyCollectionChangedEventArgs> Observe<TRow>(IReadOnlyList<TRow> rows)
    {
        List<NotifyCollectionChangedEventArgs> changes = [];
        Assert.IsInstanceOfType<INotifyCollectionChanged>(rows).CollectionChanged += (_, change) => changes.Add(change);
        return changes;
    }
}
