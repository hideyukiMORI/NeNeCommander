using System;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>
/// The one effect a bookmark-manager key or key hint has in the focused region (ADR-0079): forward
/// one existing intent to the session, move within the region, focus a region, focus the search
/// field, or nothing. The view applies it without deciding anything.
/// </summary>
public sealed record BookmarkManagerCommand
{
    private BookmarkManagerCommand(
        UserIntent? intent,
        int step,
        BookmarkManagerRegion? focusRegion)
    {
        Intent = intent;
        Step = step;
        FocusRegion = focusRegion;
    }

    /// <summary>Gets the command that does nothing, used when the matching button would be unavailable.</summary>
    public static BookmarkManagerCommand None { get; } = new(null, 0, null);

    /// <summary>Gets the command that moves focus to the search field.</summary>
    public static BookmarkManagerCommand Search { get; } = new(null, 0, null) { FocusesSearch = true };

    /// <summary>Gets the existing intent to forward to the session, when the command forwards one.</summary>
    public UserIntent? Intent { get; }

    /// <summary>Gets the movement step inside the focused region: one, minus one, or zero for none.</summary>
    public int Step { get; }

    /// <summary>Gets the region to focus, when the command moves focus to a region.</summary>
    public BookmarkManagerRegion? FocusRegion { get; }

    /// <summary>Gets whether the command moves focus to the search field.</summary>
    public bool FocusesSearch { get; private init; }

    /// <summary>Creates the command that forwards one existing intent through the session.</summary>
    /// <param name="intent">Existing intent the matching button or key already submits.</param>
    /// <returns>The forwarding command.</returns>
    public static BookmarkManagerCommand Forward(UserIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        return new BookmarkManagerCommand(intent, 0, null);
    }

    /// <summary>Creates the command that moves one item inside the focused region.</summary>
    /// <param name="step">One for the next item, minus one for the previous item.</param>
    /// <returns>The movement command.</returns>
    public static BookmarkManagerCommand Move(int step)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(Math.Abs(step), 1);
        return new BookmarkManagerCommand(null, step, null);
    }

    /// <summary>Creates the command that moves focus to one region.</summary>
    /// <param name="region">Region that receives focus.</param>
    /// <returns>The focus command.</returns>
    public static BookmarkManagerCommand Focus(BookmarkManagerRegion region)
    {
        ArgumentNullException.ThrowIfNull(region);
        return new BookmarkManagerCommand(null, 0, region);
    }
}
