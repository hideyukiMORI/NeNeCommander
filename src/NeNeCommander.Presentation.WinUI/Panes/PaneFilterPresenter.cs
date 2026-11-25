using System;
using System.Globalization;
using System.Linq;
using System.Text;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Projects inline name filtering and qualifies only its declared native exit keys.</summary>
public static class PaneFilterPresenter
{
    /// <summary>Projects the exact current query and one-time native focus effects for one pane.</summary>
    /// <param name="snapshot">Current commander state.</param>
    /// <param name="side">Permanent pane containing the control.</param>
    /// <param name="previous">Previously rendered projection, or absence at first render.</param>
    /// <returns>The immutable projection without provider work or state mutation.</returns>
    public static PaneFilterPresentation Present(CommanderSnapshot snapshot, PaneSide side, PaneFilterPresentation? previous)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(side);
        return new PaneFilterPresentation(snapshot, side, previous);
    }

    /// <summary>Qualifies mapped completion/cancellation against the exact rendered editor.</summary>
    /// <param name="intent">Intent returned by the sole keyboard mapper.</param>
    /// <param name="state">Exact rendered filter owner.</param>
    /// <param name="rawText">Native committed text captured from that exact editor control.</param>
    /// <returns>A qualified filter exit, or the unchanged intent when this editor does not own it.</returns>
    public static UserIntent Qualify(UserIntent intent, PaneFilterState state, string rawText)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(rawText);
        return state is PaneFilterEditing editing
            ? intent == UserIntent.Confirm ? UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Commit, rawText)
                : intent == UserIntent.Escape ? UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Cancel, rawText) : intent
            : intent;
    }
    /// <summary>Captures the exact editor only for native entries which leave its text control.</summary>
    /// <param name="intent">Qualified pane entry or one of the existing native command buttons.</param>
    /// <param name="state">Last rendered editor state.</param>
    /// <returns>The owner to blur before that entry, or absence for text and editor exits.</returns>
    public static PaneFilterEditing? CaptureExternalOwner(UserIntent intent, PaneFilterState state)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(state);
        return state is PaneFilterEditing editing &&
            (intent is PaneInteraction or PaneFilterEntryRequest or AddressFocusSubmission ||
                intent == UserIntent.OpenSettings || intent == UserIntent.OpenBookmarks ||
                intent == UserIntent.OpenHelp || intent == UserIntent.OpenCurrentLocationInOtherPane)
                    ? editing
                    : null;
    }

    /// <summary>Admits the exact explicit editor-exit effect even while its native textbox still owns focus.</summary>
    /// <param name="expected">Closed state returned by this render, never an unqualified pane side.</param>
    /// <param name="snapshot">Current session after synchronous native rendering callbacks.</param>
    /// <param name="context">Current native keyboard owner.</param>
    /// <returns>The current active pane to focus, or absence after replacement, blur or another input owner.</returns>
    public static PaneSide? FocusTarget(PaneFilterClosed expected, CommanderSnapshot snapshot, KeyboardContext context)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(context);
        return ReferenceEquals(snapshot.Scopes.NameFilter, expected) &&
            expected.FileListFocusSide is PaneSide side && snapshot.Panes.ActiveSide == side &&
            (context == KeyboardContext.PaneFilter || context == KeyboardContext.FileList || context == KeyboardContext.CommandEntry) &&
            PaneInputOwnership.IsAvailable(snapshot)
                ? side
                : null;
    }

    /// <summary>Resolves complete localized guidance with key caps from the canonical mapper.</summary>
    /// <param name="presentation">Current inline projection.</param>
    /// <param name="localize">The window-owned resource resolver.</param>
    /// <returns>Complete guidance without hard-coded shortcuts or ambient culture.</returns>
    public static string ComposeHint(PaneFilterPresentation presentation, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(localize);
        if (presentation.HintResourceKey != "PaneFilterEditingHint")
        {
            return localize(presentation.HintResourceKey);
        }
        CompositeFormat format = CompositeFormat.Parse(localize("PaneFilterEditingHint"));
        string commitKey = KeyboardIntentMapper.BindingsFor(KeyboardContext.PaneFilter)
            .Single(binding => binding.Intent == UserIntent.Confirm).KeyLabelResourceKey;
        string cancelKey = KeyboardIntentMapper.BindingsFor(KeyboardContext.PaneFilter)
            .Single(binding => binding.Intent == UserIntent.Escape).KeyLabelResourceKey;
        return string.Format(CultureInfo.InvariantCulture, format, localize(commitKey), localize(cancelKey));
    }

}
