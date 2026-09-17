using System;
using System.Threading;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Owns the address editor transient scope: its closed state under one lock, its admission rule,
/// and the expected-state validation of one submission. It knows no other owner, holds no freeze
/// predicate, performs no pane effect, and navigates nothing.
/// </summary>
public sealed class AddressEditorSession
{
    private readonly Lock _sync = new();
    private AddressEditorState _state = AddressEditorState.Closed;

    /// <summary>Gets the current immutable address editor state.</summary>
    public AddressEditorState Current
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    /// <summary>Decides whether one requested pane may begin editing, without changing state.</summary>
    /// <param name="panes">Pane snapshot read before any activation effect.</param>
    /// <param name="side">Pane side whose address was requested.</param>
    /// <param name="ownership">Input ownership the session derived for this scope.</param>
    /// <returns>The captured editor to open after activation, or a refusal.</returns>
    public AddressEditAdmission Admit(DualPaneSnapshot panes, PaneSide side, InteractionOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(ownership);
        lock (_sync)
        {
            return EditingSide(_state) == side || ownership == InteractionOwnership.AnotherScopeOwnsInput
                ? AddressEditAdmission.Refused
                : Capture(panes, side);
        }
    }

    private static AddressEditAdmission Capture(DualPaneSnapshot panes, PaneSide side)
    {
        return panes.Of(side).Content is PaneContentListed listed
            ? new AddressEditAdmitted(new AddressEditing(side, listed.Listing.Location))
            : AddressEditAdmission.Refused;
    }

    /// <summary>Opens the admitted editor, which the session captured before activating its pane.</summary>
    /// <param name="admitted">Admission returned by this owner for the requested side.</param>
    /// <returns>The state current after this scope opened.</returns>
    public AddressEditorState Open(AddressEditAdmitted admitted)
    {
        ArgumentNullException.ThrowIfNull(admitted);
        lock (_sync)
        {
            _state = admitted.Editor;
            return _state;
        }
    }

    /// <summary>Validates one expected-state-qualified address intent against the current state.</summary>
    /// <param name="intent">Intent offered while this scope owns precedence.</param>
    /// <param name="ownership">Input ownership the session derived for this scope.</param>
    /// <returns>The parsed target the session must navigate, or nothing to navigate.</returns>
    public AddressEditorValidation Validate(UserIntent intent, InteractionOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(ownership);
        lock (_sync)
        {
            if (intent == UserIntent.Escape)
            {
                _state = AddressEditorState.CloseFocusing(CapturedSide(_state));
                return AddressEditorValidation.NothingToNavigate;
            }
            if (intent is AddressFocusDeparture departure)
            {
                Depart(departure.ExpectedState);
                return AddressEditorValidation.NothingToNavigate;
            }
            return intent is AddressSubmission submission
                ? Submit(submission, ownership)
                : AddressEditorValidation.NothingToNavigate;
        }
    }

    private void Depart(AddressEditorState expected)
    {
        if (ReferenceEquals(_state, expected))
        {
            _state = AddressEditorState.Closed;
        }
    }

    private AddressEditorValidation Submit(AddressSubmission submission, InteractionOwnership ownership)
    {
        if (!ReferenceEquals(_state, submission.ExpectedState) ||
            ownership == InteractionOwnership.AnotherScopeOwnsInput)
        {
            return AddressEditorValidation.NothingToNavigate;
        }
        PaneSide side = CapturedSide(_state);
        FileSystemPath original = CapturedLocation(_state);
        PathParseOutcome outcome = FileSystemPath.Parse(submission.RawText);
        if (outcome is PathParseFailure failure)
        {
            _state = new AddressInputRejected(side, original, submission.RawText, failure.Kind);
            return AddressEditorValidation.NothingToNavigate;
        }
        _state = AddressEditorState.CloseFocusing(side);
        return new AddressTargetAccepted(side, ((PathParseSuccess)outcome).Path);
    }

    private static PaneSide? EditingSide(AddressEditorState state)
    {
        return state switch
        {
            AddressEditing editing => editing.Side,
            AddressInputRejected rejected => rejected.Side,
            _ => null,
        };
    }

    private static PaneSide CapturedSide(AddressEditorState state)
    {
        return state is AddressEditing editing ? editing.Side : ((AddressInputRejected)state).Side;
    }

    private static FileSystemPath CapturedLocation(AddressEditorState state)
    {
        return state is AddressEditing editing
            ? editing.OriginalLocation
            : ((AddressInputRejected)state).OriginalLocation;
    }
}
