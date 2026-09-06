using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>Render-ready search, category filters, and ordered bookmark rows.</summary>
public sealed record BookmarkBrowsePresentation
{
    private readonly ReadOnlyCollection<BookmarkCategoryOption> _categories;
    private readonly ReadOnlyCollection<BookmarkRow> _rows;

    internal BookmarkBrowsePresentation(
        string searchText,
        IReadOnlyList<BookmarkCategoryOption> categories,
        IReadOnlyList<BookmarkRow> rows,
        BookmarkRow? selectedRow)
    {
        ArgumentNullException.ThrowIfNull(searchText);
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(rows);
        SearchText = searchText;
        _categories = new List<BookmarkCategoryOption>(categories).AsReadOnly();
        _rows = new List<BookmarkRow>(rows).AsReadOnly();
        SelectedRow = selectedRow;
    }

    /// <summary>Gets the verbatim search text retained by the session.</summary>
    public string SearchText { get; }
    /// <summary>Gets All, Uncategorized, and user categories in display order.</summary>
    public IReadOnlyList<BookmarkCategoryOption> Categories => _categories;
    /// <summary>Gets the filtered bookmarks in catalog order.</summary>
    public IReadOnlyList<BookmarkRow> Rows => _rows;
    /// <summary>Gets the selected visible row, when present.</summary>
    public BookmarkRow? SelectedRow { get; }
}
