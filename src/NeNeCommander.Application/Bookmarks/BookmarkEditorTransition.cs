using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the closed effects of applying one bookmark-manager action.</summary>
internal abstract record BookmarkEditorTransition
{
    private protected BookmarkEditorTransition()
    {
    }

    internal sealed record StateChanged : BookmarkEditorTransition;

    internal sealed record CloseRequested : BookmarkEditorTransition;

    internal sealed record CatalogChanged : BookmarkEditorTransition
    {
        internal CatalogChanged(BookmarkCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(catalog);
            Catalog = catalog;
        }

        internal BookmarkCatalog Catalog { get; }
    }
}
