using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>
/// Owns the bookmark view's stable row sources. Selection and focus marks replace only affected
/// rows; catalog changes retain complete stale-action captures without resetting either list.
/// </summary>
internal sealed class BookmarkBrowseRows
{
    private readonly ObservableCollection<BookmarkCategoryOption> _categories = [];
    private readonly ObservableCollection<BookmarkRow> _rows = [];

    internal BookmarkBrowseRows()
    {
        Categories = new ReadOnlyObservableCollection<BookmarkCategoryOption>(_categories);
        Rows = new ReadOnlyObservableCollection<BookmarkRow>(_rows);
    }

    internal IReadOnlyList<BookmarkCategoryOption> Categories { get; }

    internal IReadOnlyList<BookmarkRow> Rows { get; }

    internal BookmarkCategoryOption? SelectedCategory => _categories.FirstOrDefault(category => category.IsSelected);

    internal BookmarkRow? SelectedRow => _rows.LastOrDefault(row => row.IsSelected);

    internal void Update(BookmarkBrowsePresentation browse)
    {
        ArgumentNullException.ThrowIfNull(browse);
        UpdateRows(_categories, browse.Categories, SameCategory);
        UpdateRows(_rows, browse.Rows, SameRow);
    }

    private static void UpdateRows<TRow>(
        ObservableCollection<TRow> current,
        IReadOnlyList<TRow> incoming,
        Func<TRow, TRow, bool> same)
    {
        for (int index = 0; index < incoming.Count; index++)
        {
            if (index >= current.Count)
            {
                current.Add(incoming[index]);
            }
            else if (!same(current[index], incoming[index]))
            {
                current[index] = incoming[index];
            }
        }
        while (current.Count > incoming.Count)
        {
            current.RemoveAt(current.Count - 1);
        }
    }

    private static bool SameCategory(BookmarkCategoryOption left, BookmarkCategoryOption right)
    {
        return left.Filter == right.Filter &&
            left.DisplayText == right.DisplayText &&
            left.IsSelected == right.IsSelected &&
            left.Mark == right.Mark &&
            left.EntryCount == right.EntryCount &&
            SameCapture(left.Selection, right.Selection);
    }

    private static bool SameRow(BookmarkRow left, BookmarkRow right)
    {
        return left.Selection == right.Selection &&
            left.CategoryText == right.CategoryText &&
            left.ShortcutLabelResourceKey == right.ShortcutLabelResourceKey &&
            left.IsSelected == right.IsSelected && left.Mark == right.Mark;
    }

    private static bool SameCapture(BookmarkCategorySelection? left, BookmarkCategorySelection? right)
    {
        return ReferenceEquals(left, right) ||
            (left is not null && right is not null &&
                left.Category == right.Category && left.Entries.SequenceEqual(right.Entries));
    }
}
