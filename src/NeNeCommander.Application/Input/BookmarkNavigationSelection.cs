using System;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Input;

/// <summary>Requests navigation from one complete immutable manager selection.</summary>
public sealed record BookmarkNavigationSelection : UserIntent
{
    internal BookmarkNavigationSelection(BookmarkSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        Selection = selection;
    }

    /// <summary>Gets the selected key and complete expected entry.</summary>
    public BookmarkSelection Selection { get; }
}
