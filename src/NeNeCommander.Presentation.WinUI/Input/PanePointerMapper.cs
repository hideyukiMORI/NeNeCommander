using System;
using System.Linq;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Panes;
using NeNeCommander.Presentation.WinUI.Commands;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Maps pane gestures to qualified intents and owns the single accepted double-tap candidate.</summary>
public sealed class PanePointerMapper
{
    private PanePointerCandidate? _candidate;

    /// <summary>Forgets a gesture when other input, rejected admission or window deactivation intervenes.</summary>
    public void Reset()
    {
        _candidate = null;
    }

    /// <summary>Maps an owned row tap; absence means this gesture has no declared mapping.</summary>
    public UserIntent? MapRowTap(PaneSide side, PanePresentation presentation, PaneRow row, PanePointerModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(modifier);
        Reset();
        PaneEntryAction? action = SelectionAction(modifier);
        return action is not null && presentation.Rows.Any(candidate => ReferenceEquals(candidate, row))
            ? UserIntent.InteractWithPaneEntry(side, presentation.SourceSnapshot, row.Entry, action)
            : null;
    }

    /// <summary>Maps a passive surface tap without deriving an entry from native selection.</summary>
    public UserIntent? MapSurfaceTap(PaneSide side, PanePresentation presentation, PanePointerSurface surface, PanePointerModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(modifier);
        Reset();
        return modifier == PanePointerModifier.None
            ? UserIntent.InteractWithPaneSurface(side, presentation.SourceSnapshot,
                surface == PanePointerSurface.Header ? PaneSurfaceAction.Activate : PaneSurfaceAction.ClearSelection)
            : null;
    }

    /// <summary>Maps a context request only for an exact row of its rendered projection.</summary>
    public UserIntent? MapRowContext(PaneSide side, PanePresentation presentation, PaneRow row)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(row);
        Reset();
        return presentation.Rows.Any(candidate => ReferenceEquals(candidate, row))
            ? UserIntent.InteractWithPaneEntry(side, presentation.SourceSnapshot, row.Entry, PaneEntryAction.OpenContextMenu)
            : null;
    }

    /// <summary>Maps a passive background or header context without changing its focus or selection.</summary>
    public UserIntent MapSurfaceContext(PaneSide side, PanePresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(presentation);
        Reset();
        return UserIntent.InteractWithPaneSurface(side, presentation.SourceSnapshot, PaneSurfaceAction.OpenContextMenu);
    }

    /// <summary>Maps a parent button with its explicit pane and exact rendered owner.</summary>
    public UserIntent MapParentNavigation(PaneSide side, PanePresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(presentation);
        Reset();
        return UserIntent.InteractWithPaneSurface(side, presentation.SourceSnapshot, PaneSurfaceAction.NavigateParent);
    }

    /// <summary>Consumes an accepted first tap once and maps only the same listing-owned entry.</summary>
    public UserIntent? MapRowDoubleTap(PaneSide side, PanePresentation presentation, PaneRow row, PanePointerModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(modifier);
        PanePointerCandidate? candidate = _candidate;
        Reset();
        return modifier == PanePointerModifier.None && candidate is not null &&
            candidate.Side == side && ReferenceEquals(candidate.Entry, row.Entry) &&
            presentation.SourceSnapshot.Content is PaneContentListed listed &&
            ReferenceEquals(candidate.Listing, listed.Listing) &&
            presentation.Rows.Any(owned => ReferenceEquals(owned, row))
                ? UserIntent.InteractWithPaneEntry(side, presentation.SourceSnapshot, row.Entry, PaneEntryAction.Open)
                : null;
    }

    /// <summary>
    /// Observes the actual snapshots immediately before admitted handling and after its result.
    /// A stale request cannot seed a double tap or request focus. Navigation completion never requests focus.
    /// </summary>
    /// <returns>The admitted synchronous pointer target to focus, or null without such a receipt.</returns>
    public PaneSide? ObserveResult(UserIntent intent, CommanderSnapshot before, CommanderSnapshot after)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        Reset();
        if (intent is PaneRowInteraction row && row.Action == PaneEntryAction.Select)
        {
            _candidate = AcceptedCandidate(row, before, after);
        }
        ObserveSession(after);
        return intent is PaneInteraction interaction &&
            (interaction is not PaneRowInteraction rowAction || rowAction.Action != PaneEntryAction.Open) &&
            (interaction is not PaneSurfaceInteraction surface || surface.Action != PaneSurfaceAction.NavigateParent) &&
            ReferenceEquals(PaneOf(before.Panes, interaction.Side), interaction.ExpectedSnapshot) &&
            !ReferenceEquals(PaneOf(before.Panes, interaction.Side), PaneOf(after.Panes, interaction.Side)) &&
            after.Panes.ActiveSide == interaction.Side && PaneInputOwnership.IsAvailable(after)
                ? interaction.Side
                : null;
    }

    /// <summary>Admits a captured focus effect only while its active pane and native input context still own it.</summary>
    /// <param name="snapshot">Current session after rendering and any synchronous native callback.</param>
    /// <param name="side">The accepted pane focus target.</param>
    /// <param name="context">Current native keyboard owner.</param>
    /// <returns>Whether the host may apply the native focus effect now.</returns>
    public static bool CanReturnFocus(CommanderSnapshot snapshot, PaneSide side, KeyboardContext context)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(context);
        return snapshot.Panes.ActiveSide == side &&
            (context == KeyboardContext.FileList || context == KeyboardContext.CommandEntry) &&
            CommandEntryPresenter.Present(snapshot).IsEnabled;
    }

    private static PanePointerCandidate? AcceptedCandidate(PaneRowInteraction row, CommanderSnapshot before, CommanderSnapshot after)
    {
        PaneSnapshot previous = PaneOf(before.Panes, row.Side);
        PaneSnapshot current = PaneOf(after.Panes, row.Side);
        return !ReferenceEquals(previous, current) && after.Panes.ActiveSide == row.Side &&
            ReferenceEquals(previous, row.ExpectedSnapshot) &&
            current.Content is PaneContentListed listed &&
            row.ExpectedSnapshot.Content is PaneContentListed expected &&
            ReferenceEquals(listed.Listing, expected.Listing) &&
            ReferenceEquals(listed.State.FocusItem, row.Entry.Path)
                ? new PanePointerCandidate(row.Side, listed.Listing, row.Entry)
                : null;
    }

    /// <summary>Expires a candidate when its listing, active pane or input owner changes.</summary>
    public void ObserveSession(CommanderSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (_candidate is not null &&
            (!PaneInputOwnership.IsAvailable(snapshot) || snapshot.Panes.ActiveSide != _candidate.Side ||
                PaneOf(snapshot.Panes, _candidate.Side).Content is not PaneContentListed listed ||
                !ReferenceEquals(listed.Listing, _candidate.Listing)))
        {
            Reset();
        }
    }

    private static PaneSnapshot PaneOf(DualPaneSnapshot panes, PaneSide side)
    {
        return side == PaneSide.Left ? panes.Left : panes.Right;
    }

    private static PaneEntryAction? SelectionAction(PanePointerModifier modifier)
    {
        return modifier switch
        {
            _ when modifier == PanePointerModifier.None => PaneEntryAction.Select,
            _ when modifier == PanePointerModifier.Control => PaneEntryAction.ToggleSelection,
            _ when modifier == PanePointerModifier.Shift => PaneEntryAction.SelectRange,
            _ when modifier == PanePointerModifier.ControlShift => PaneEntryAction.AddRange,
            _ => null,
        };
    }
}
