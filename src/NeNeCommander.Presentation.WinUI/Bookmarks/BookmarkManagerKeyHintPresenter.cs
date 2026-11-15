using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>
/// Projects the bookmark manager's key band and its keyboard-help rows from the canonical bookmark
/// table only (ADR-0079, KBD-005). It owns order and wording, never a binding: every key cap is
/// read from <see cref="KeyboardIntentMapper.BookmarkManagerBindings"/>, so a hint can only
/// advertise a key the mapper performs. The band's wording follows the focused region.
/// </summary>
public static class BookmarkManagerKeyHintPresenter
{
    private static readonly BookmarkManagerKeyAction[] BandOrder =
    [
        BookmarkManagerKeyAction.Activate,
        BookmarkManagerKeyAction.FocusSearch,
        BookmarkManagerKeyAction.SwitchRegion,
        BookmarkManagerKeyAction.Add,
        BookmarkManagerKeyAction.Edit,
        BookmarkManagerKeyAction.Delete,
        BookmarkManagerKeyAction.Close,
    ];

    /// <summary>Projects the band for the focused region: movement first, then each command once.</summary>
    /// <param name="region">Region that holds focus.</param>
    /// <returns>The ordered hints.</returns>
    public static IReadOnlyList<BookmarkManagerKeyHint> Present(BookmarkManagerRegion region)
    {
        ArgumentNullException.ThrowIfNull(region);
        List<BookmarkManagerKeyHint> hints =
        [
            new(
                [FirstCap(BookmarkManagerKeyAction.MoveNext), FirstCap(BookmarkManagerKeyAction.MovePrevious)],
                "BookmarkHintMove",
                BookmarkManagerKeyAction.MoveNext,
                "BookmarkHintMove"),
        ];
        foreach (BookmarkManagerKeyAction action in BandOrder)
        {
            string label = BandLabel(region, action);
            hints.Add(new BookmarkManagerKeyHint([FirstCap(action)], label, action, AutomationIdOf(action)));
        }
        return hints.AsReadOnly();
    }

    /// <summary>Gets the region-neutral keyboard-help label of one action.</summary>
    /// <param name="action">Declared bookmark-manager action.</param>
    /// <returns>The localized label resource.</returns>
    internal static string HelpLabelOf(BookmarkManagerKeyAction action)
    {
        return action switch
        {
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.MoveNext => "IntentLabelMoveNext",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.MovePrevious => "IntentLabelMovePrevious",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Activate => "BookmarkHelpActivate",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.FocusSearch => "BookmarkHelpSearch",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.SwitchRegion => "BookmarkHelpSwitch",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Add => "BookmarkHelpAdd",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Edit => "BookmarkHelpEdit",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Delete => "BookmarkHelpDelete",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Close => "BookmarkHintClose",
            _ => "BookmarkHelpLeaveSearch",
        };
    }

    private static string BandLabel(BookmarkManagerRegion region, BookmarkManagerKeyAction action)
    {
        bool categories = region == BookmarkManagerRegion.Categories;
        return action switch
        {
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Activate =>
                categories ? "BookmarkHintToList" : "BookmarkHintOpen",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Add =>
                categories ? "BookmarkHintAddCategory" : "BookmarkHintAddBookmark",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Edit =>
                categories ? "BookmarkHintRenameCategory" : "BookmarkHintEditBookmark",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Delete =>
                categories ? "BookmarkHintDeleteCategory" : "BookmarkHintDeleteBookmark",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.FocusSearch => "BookmarkHintSearch",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.SwitchRegion => "BookmarkHintSwitch",
            _ => "BookmarkHintClose",
        };
    }

    private static string AutomationIdOf(BookmarkManagerKeyAction action)
    {
        return action switch
        {
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Activate => "BookmarkHintActivate",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.FocusSearch => "BookmarkHintSearch",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.SwitchRegion => "BookmarkHintSwitch",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Add => "BookmarkHintAdd",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Edit => "BookmarkHintEdit",
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Delete => "BookmarkHintDelete",
            _ => "BookmarkHintClose",
        };
    }

    private static string FirstCap(BookmarkManagerKeyAction action)
    {
        return KeyboardIntentMapper.BookmarkManagerBindings
            .First(binding => binding.Context == KeyboardContext.Bookmarks && binding.Action == action)
            .KeyLabelResourceKey;
    }
}
