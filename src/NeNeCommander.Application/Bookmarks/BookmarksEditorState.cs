namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the closed session-owned bookmark-manager interaction states.</summary>
public abstract record BookmarksEditorState
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the state used while the bookmark manager does not own modal input.</summary>
    public static BookmarksEditorState Closed { get; } = new BookmarksEditorClosed();

    private protected BookmarksEditorState()
    {
    }
}
