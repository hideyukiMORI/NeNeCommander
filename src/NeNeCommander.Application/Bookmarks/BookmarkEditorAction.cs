using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the closed actions accepted by the session-owned bookmark manager.</summary>
public abstract record BookmarkEditorAction
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the action that validates and saves the current draft.</summary>
    public static BookmarkEditorAction Save { get; } = new SaveAction();
    /// <summary>Gets the action that starts registration with session-derived defaults.</summary>
    public static BookmarkEditorAction BeginAddBookmark { get; } = new BeginAddBookmarkAction();
    /// <summary>Gets the action that starts a new category draft.</summary>
    public static BookmarkEditorAction BeginAddCategory { get; } = new BeginAddCategoryAction();
    /// <summary>Gets the action that confirms the visible category deletion question.</summary>
    public static BookmarkEditorAction ConfirmDeleteCategory { get; } =
        new ConfirmDeleteCategoryAction();
    /// <summary>Gets the action that cancels the deepest visible manager state.</summary>
    public static BookmarkEditorAction Cancel { get; } = new CancelAction();

    private protected BookmarkEditorAction()
    {
    }

    /// <summary>Creates an action that replaces the manager search text.</summary>
    public static BookmarkEditorAction Search(string text)
    {
        return new SearchAction(text);
    }

    /// <summary>Creates an action that selects one closed category filter.</summary>
    public static BookmarkEditorAction Filter(BookmarkCategoryFilter filter)
    {
        return new FilterAction(filter);
    }

    /// <summary>Creates an action that selects one complete displayed entry or clears selection.</summary>
    public static BookmarkEditorAction Select(BookmarkSelection? selection)
    {
        return new SelectAction(selection);
    }

    /// <summary>Creates an action that starts editing a complete displayed entry.</summary>
    public static BookmarkEditorAction BeginEditBookmark(BookmarkSelection selection)
    {
        return new BeginEditBookmarkAction(selection);
    }

    /// <summary>Creates an action that replaces the current untrusted bookmark draft.</summary>
    public static BookmarkEditorAction UpdateBookmark(BookmarkDraft draft)
    {
        return new UpdateBookmarkAction(draft);
    }

    /// <summary>Creates an action that starts renaming a complete displayed category.</summary>
    public static BookmarkEditorAction BeginRenameCategory(BookmarkCategorySelection selection)
    {
        return new BeginRenameCategoryAction(selection);
    }

    /// <summary>Creates an action that replaces the current untrusted category name.</summary>
    public static BookmarkEditorAction UpdateCategory(string name)
    {
        return new UpdateCategoryAction(name);
    }

    /// <summary>Creates an action that deletes one unchanged displayed bookmark.</summary>
    public static BookmarkEditorAction DeleteBookmark(BookmarkSelection selection)
    {
        return new DeleteBookmarkAction(selection);
    }

    /// <summary>Creates an action that opens category deletion confirmation.</summary>
    public static BookmarkEditorAction BeginDeleteCategory(BookmarkCategorySelection selection)
    {
        return new BeginDeleteCategoryAction(selection);
    }

    internal sealed record SearchAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }

        internal SearchAction(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            Text = text;
        }

        internal string Text { get; }
    }

    internal sealed record FilterAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }

        internal FilterAction(BookmarkCategoryFilter filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            SelectedFilter = filter;
        }

        internal BookmarkCategoryFilter SelectedFilter { get; }
    }

    internal sealed record SelectAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }

        internal SelectAction(BookmarkSelection? selection)
        {
            Selection = selection;
        }

        internal BookmarkSelection? Selection { get; }
    }

    internal sealed record BeginAddBookmarkAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    internal sealed record BeginEditBookmarkAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }

        internal BeginEditBookmarkAction(BookmarkSelection selection)
        {
            ArgumentNullException.ThrowIfNull(selection);
            Selection = selection;
        }

        internal BookmarkSelection Selection { get; }
    }

    internal sealed record UpdateBookmarkAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }

        internal UpdateBookmarkAction(BookmarkDraft draft)
        {
            ArgumentNullException.ThrowIfNull(draft);
            Draft = draft;
        }

        internal BookmarkDraft Draft { get; }
    }

    internal sealed record SaveAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    internal sealed record BeginAddCategoryAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    internal sealed record BeginRenameCategoryAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }

        internal BeginRenameCategoryAction(BookmarkCategorySelection selection)
        {
            ArgumentNullException.ThrowIfNull(selection);
            Selection = selection;
        }

        internal BookmarkCategorySelection Selection { get; }
    }

    internal sealed record UpdateCategoryAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }

        internal UpdateCategoryAction(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            Name = name;
        }

        internal string Name { get; }
    }

    internal sealed record DeleteBookmarkAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }

        internal DeleteBookmarkAction(BookmarkSelection selection)
        {
            ArgumentNullException.ThrowIfNull(selection);
            Selection = selection;
        }

        internal BookmarkSelection Selection { get; }
    }

    internal sealed record BeginDeleteCategoryAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }

        internal BeginDeleteCategoryAction(BookmarkCategorySelection selection)
        {
            ArgumentNullException.ThrowIfNull(selection);
            Selection = selection;
        }

        internal BookmarkCategorySelection Selection { get; }
    }

    internal sealed record ConfirmDeleteCategoryAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    internal sealed record CancelAction : BookmarkEditorAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
