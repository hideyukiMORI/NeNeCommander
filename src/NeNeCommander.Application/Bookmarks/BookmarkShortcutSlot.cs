using System.Collections.Generic;
using System.Linq;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Identifies one of the nine fixed direct-navigation bookmark slots.</summary>
public abstract record BookmarkShortcutSlot
{
    /// <summary>Gets slot 1.</summary>
    public static BookmarkShortcutSlot One { get; } = new Slot(1);
    /// <summary>Gets slot 2.</summary>
    public static BookmarkShortcutSlot Two { get; } = new Slot(2);
    /// <summary>Gets slot 3.</summary>
    public static BookmarkShortcutSlot Three { get; } = new Slot(3);
    /// <summary>Gets slot 4.</summary>
    public static BookmarkShortcutSlot Four { get; } = new Slot(4);
    /// <summary>Gets slot 5.</summary>
    public static BookmarkShortcutSlot Five { get; } = new Slot(5);
    /// <summary>Gets slot 6.</summary>
    public static BookmarkShortcutSlot Six { get; } = new Slot(6);
    /// <summary>Gets slot 7.</summary>
    public static BookmarkShortcutSlot Seven { get; } = new Slot(7);
    /// <summary>Gets slot 8.</summary>
    public static BookmarkShortcutSlot Eight { get; } = new Slot(8);
    /// <summary>Gets slot 9.</summary>
    public static BookmarkShortcutSlot Nine { get; } = new Slot(9);

    /// <summary>Gets every fixed slot in numeric order.</summary>
    public static IReadOnlyList<BookmarkShortcutSlot> All { get; } =
    [
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
    ];

    private BookmarkShortcutSlot(int number)
    {
        Number = number;
    }

    /// <summary>Gets the persisted slot number.</summary>
    public int Number { get; }

    /// <summary>Parses an untrusted integer into one of the nine closed slots.</summary>
    public static BookmarkShortcutSlotParseOutcome Parse(int number)
    {
        BookmarkShortcutSlot? slot = All.FirstOrDefault(candidate => candidate.Number == number);
        return slot is null
            ? new BookmarkShortcutSlotRejected()
            : new BookmarkShortcutSlotAccepted(slot);
    }

    private sealed record Slot : BookmarkShortcutSlot
    {
        internal Slot(int number)
            : base(number)
        {
        }
    }
}
