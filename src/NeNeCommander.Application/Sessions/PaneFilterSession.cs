using System.Threading;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Owns only filter editing and exact-state admission; panes own every applied query.</summary>
public sealed class PaneFilterSession
{
    private readonly Lock _sync = new();
    private PaneFilterState _state = PaneFilterState.Closed;

    /// <summary>Gets the immutable current editor state.</summary>
    public PaneFilterState Current
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    internal void Open(DualPaneSnapshot panes, InteractionOwnership ownership)
    {
        lock (_sync)
        {
            if (_state is PaneFilterClosed && CanEdit(panes, ownership))
            {
                _state = new PaneFilterEditing(panes.ActiveSide, panes.Of(panes.ActiveSide));
            }
        }
    }

    internal PaneFilterValidation Clear(DualPaneSnapshot panes, InteractionOwnership ownership)
    {
        lock (_sync)
        {
            if (_state is not PaneFilterClosed || !CanEdit(panes, ownership))
            {
                return PaneFilterValidation.NothingToApply;
            }
            PaneFilterEditing captured = new(panes.ActiveSide, panes.Of(panes.ActiveSide));
            _state = PaneFilterState.CloseFocusing(captured.Side);
            return new PaneFilterQueryAccepted(captured, PaneNameFilterQuery.Empty);
        }
    }

    internal PaneFilterValidation Validate(UserIntent intent, DualPaneSnapshot panes, InteractionOwnership ownership)
    {
        lock (_sync)
        {
            if (_state is not PaneFilterEditing editing)
            {
                return PaneFilterValidation.NothingToApply;
            }
            bool ownsPane = CanEdit(panes, ownership) && panes.ActiveSide == editing.Side &&
                ReferenceEquals(panes.Of(panes.ActiveSide), editing.SourcePane);
            if (intent is PaneFilterEditSubmission blur && blur.Action == PaneFilterEditAction.Blur &&
                ReferenceEquals(blur.ExpectedState, editing))
            {
                _state = PaneFilterState.Closed;
                return ownsPane
                    ? new PaneFilterQueryAccepted(editing, PaneNameFilterQuery.Parse(blur.RawText))
                    : PaneFilterValidation.NothingToApply;
            }
            return ownsPane ? ValidateCurrent(intent, editing) : PaneFilterValidation.NothingToApply;
        }
    }

    private PaneFilterValidation ValidateCurrent(UserIntent intent, PaneFilterEditing editing)
    {
        if (intent is PaneFilterTextSubmission text && ReferenceEquals(text.ExpectedState, editing))
        {
            return new PaneFilterQueryAccepted(editing, PaneNameFilterQuery.Parse(text.RawText));
        }
        if (intent is PaneFilterEditSubmission exit && ReferenceEquals(exit.ExpectedState, editing))
        {
            _state = PaneFilterState.CloseFocusing(editing.Side);
            PaneNameFilterQuery query = exit.Action == PaneFilterEditAction.Cancel
                ? PaneNameFilterQuery.Empty
                : PaneNameFilterQuery.Parse(exit.RawText);
            return new PaneFilterQueryAccepted(editing, query);
        }
        return PaneFilterValidation.NothingToApply;
    }

    internal void Reconcile(PaneFilterQueryAccepted accepted, PaneSnapshot pane)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_state, accepted.Editor) &&
                !ReferenceEquals(pane, accepted.Editor.SourcePane) &&
                pane.Content is PaneContentListed listed && listed.State.NameFilter.Text == accepted.Query.Text)
            {
                _state = new PaneFilterEditing(accepted.Editor.Side, pane);
            }
        }
    }

    private static bool CanEdit(DualPaneSnapshot panes, InteractionOwnership ownership)
    {
        return ownership == InteractionOwnership.ScopeOwnsInput &&
            panes.CanInteractWithPanes && panes.Of(panes.ActiveSide).Content is PaneContentListed;
    }
}
