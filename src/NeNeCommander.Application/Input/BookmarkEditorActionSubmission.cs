using System;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Input;

/// <summary>Carries one typed bookmark-manager action through the sole command route.</summary>
public sealed record BookmarkEditorActionSubmission : UserIntent
{
    private protected override void RestrictDerivation()
    {
    }

    internal BookmarkEditorActionSubmission(BookmarkEditorAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Action = action;
    }

    /// <summary>Gets the typed manager action.</summary>
    public BookmarkEditorAction Action { get; }
}
