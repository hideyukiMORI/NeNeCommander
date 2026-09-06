using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents stale-safe admission to manager navigation.</summary>
internal abstract record BookmarkNavigationStart
{
    private protected BookmarkNavigationStart()
    {
    }

    internal sealed record Rejected : BookmarkNavigationStart;

    internal sealed record Accepted : BookmarkNavigationStart
    {
        internal Accepted(BookmarkEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            Entry = entry;
        }

        internal BookmarkEntry Entry { get; }
    }
}
