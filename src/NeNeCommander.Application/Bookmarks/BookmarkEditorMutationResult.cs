using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Pairs one validated editor state with its closed catalog transition.</summary>
internal sealed record BookmarkEditorMutationResult
{
    internal BookmarkEditorMutationResult(
        BookmarksEditorState state,
        BookmarkEditorTransition transition)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(transition);
        State = state;
        Transition = transition;
    }

    internal BookmarksEditorState State { get; }

    internal BookmarkEditorTransition Transition { get; }
}
