using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Commands;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Windowing;

namespace NeNeCommander.Presentation.WinUI.Help;

/// <summary>
/// Projects the keyboard help from the canonical key map only (ADR-0076). It owns grouping, order
/// and labels, never a binding: the file-list, Locations and help groups read
/// <see cref="KeyboardIntentMapper.BindingsFor"/>, the window and palette groups reuse their key
/// hint projections, and every alias of one action shares one row. The <c>gg</c> chord, the only
/// stateful entry, is shown just before <c>G</c>, and the nine bookmark slots share one row.
/// </summary>
public static class KeyboardHelpPresenter
{
    private const string ChordKeyLabel = "KeyLabelChordGG";
    private const string BookmarkSlotsKeyLabel = "KeyLabelCtrl1To9";
    private const string BookmarkSlotsLabel = "IntentLabelBookmarkSlots";

    /// <summary>
    /// Gets the help in display order: the file list in the first column, then window adjustment,
    /// Locations, settings, the command palette and the help itself in the second column.
    /// </summary>
    /// <returns>The two-column help content.</returns>
    public static KeyboardHelpPresentation Present()
    {
        List<KeyboardHelpGroup> primary = [new("KeyboardHelpGroupFileList", FileListRows())];
        List<KeyboardHelpGroup> secondary =
        [
            new("KeyboardHelpGroupWindowAdjustment", WindowAdjustmentRows()),
            new("KeyboardHelpGroupLocations", ContextRows(KeyboardContext.Locations, LabelOf)),
            new("KeyboardHelpGroupSettings", ContextRows(KeyboardContext.Settings, LabelOf)),
            new("KeyboardHelpGroupCommandPalette", CommandPaletteRows()),
            new("KeyboardHelpGroupHelp", ContextRows(KeyboardContext.KeyboardHelp, HelpLabelOf)),
        ];
        return new KeyboardHelpPresentation(primary.AsReadOnly(), secondary.AsReadOnly());
    }

    private static ReadOnlyCollection<KeyboardHelpRow> FileListRows()
    {
        RowBuilder rows = new();
        foreach (KeyBinding binding in KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList))
        {
            if (binding.Intent is BookmarkShortcutSelection)
            {
                rows.AddOnce(BookmarkSlotsLabel, BookmarkSlotsKeyLabel);
                continue;
            }
            if (binding.Intent == UserIntent.FocusLast)
            {
                rows.AddOnce(LabelOf(UserIntent.FocusFirst), ChordKeyLabel);
            }
            rows.Add(LabelOf(binding.Intent), binding.KeyLabelResourceKey);
        }
        return rows.Build();
    }

    private static ReadOnlyCollection<KeyboardHelpRow> ContextRows(
        KeyboardContext context,
        Func<UserIntent, string> label)
    {
        RowBuilder rows = new();
        foreach (KeyBinding binding in KeyboardIntentMapper.BindingsFor(context))
        {
            rows.Add(label(binding.Intent), binding.KeyLabelResourceKey);
        }
        return rows.Build();
    }

    private static ReadOnlyCollection<KeyboardHelpRow> WindowAdjustmentRows()
    {
        return WindowAdjustmentKeyHintPresenter.Present()
            .Select(hint => new KeyboardHelpRow(hint.KeyLabelResourceKeys, hint.IntentLabelResourceKey))
            .ToList()
            .AsReadOnly();
    }

    private static ReadOnlyCollection<KeyboardHelpRow> CommandPaletteRows()
    {
        RowBuilder rows = new();
        foreach (CommandPaletteKeyHint hint in CommandPaletteKeyHintPresenter.Present())
        {
            rows.Add(hint.IntentLabelResourceKey, hint.KeyLabelResourceKey);
        }
        return rows.Build();
    }

    private static string LabelOf(UserIntent intent)
    {
        return CommandLabelCatalog.LabelFor(intent).ResourceKey;
    }

    private static string HelpLabelOf(UserIntent intent)
    {
        return intent == UserIntent.MoveNext ? "KeyboardHelpScrollDown"
            : intent == UserIntent.MovePrevious ? "KeyboardHelpScrollUp"
            : intent == UserIntent.MoveHalfPageDown ? "KeyboardHelpPageDown"
            : intent == UserIntent.MoveHalfPageUp ? "KeyboardHelpPageUp"
            : "KeyboardHelpClose";
    }

    /// <summary>Collects caps under their action label, keeping the first-seen order of actions.</summary>
    private sealed class RowBuilder
    {
        private readonly List<string> _order = [];
        private readonly Dictionary<string, List<string>> _caps = [];

        internal void Add(string label, string cap)
        {
            if (!_caps.TryGetValue(label, out List<string>? caps))
            {
                caps = [];
                _caps.Add(label, caps);
                _order.Add(label);
            }
            if (!caps.Contains(cap))
            {
                caps.Add(cap);
            }
        }

        internal void AddOnce(string label, string cap)
        {
            if (!_caps.ContainsKey(label))
            {
                Add(label, cap);
            }
        }

        internal ReadOnlyCollection<KeyboardHelpRow> Build()
        {
            return _order
                .Select(label => new KeyboardHelpRow(_caps[label].AsReadOnly(), label))
                .ToList()
                .AsReadOnly();
        }
    }
}
