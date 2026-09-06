using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Retains untrusted bookmark form text until the session owner validates Save.</summary>
public sealed record BookmarkDraft
{
    /// <summary>Initializes one complete form draft.</summary>
    public BookmarkDraft(
        string name,
        string path,
        BookmarkCategoryFilter category,
        BookmarkShortcutSlot? shortcut)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(category);
        Name = name;
        Path = path;
        Category = category;
        Shortcut = shortcut;
    }

    /// <summary>Gets the untrusted display-name text.</summary>
    public string Name { get; }

    /// <summary>Gets the untrusted path text.</summary>
    public string Path { get; }

    /// <summary>Gets the selected category; All is rejected at Save.</summary>
    public BookmarkCategoryFilter Category { get; }

    /// <summary>Gets the optional fixed shortcut assignment.</summary>
    public BookmarkShortcutSlot? Shortcut { get; }
}
