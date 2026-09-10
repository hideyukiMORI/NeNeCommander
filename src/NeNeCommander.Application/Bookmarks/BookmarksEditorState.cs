namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the closed session-owned bookmark-manager interaction states.</summary>
public abstract record BookmarksEditorState
{
    /// <summary>Gets the state used while the bookmark manager does not own modal input.</summary>
    public static BookmarksEditorState Closed { get; } = new BookmarksEditorClosed();

    private protected BookmarksEditorState()
    {
    }
}
