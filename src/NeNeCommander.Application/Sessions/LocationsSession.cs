using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Locations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Wsl;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Owns the Locations picker transient scope: its closed state under one lock, its admission rule,
/// the concurrent read of both sections, focus movement, and the expected-state validation of one
/// qualified selection or cancellation. It knows no other owner, holds no freeze predicate, performs
/// no pane effect, and dispatches nothing. Every operation is total: on a state it does not expect
/// it changes nothing.
/// </summary>
public sealed class LocationsSession
{
    private readonly Lock _sync = new();
    private readonly IDriveCatalog _drives;
    private readonly IWslDistributionCatalog _wslRoots;
    private LocationsState _state = LocationsState.Closed;

    /// <summary>Initializes the scope owner over the two read-only catalogs it lists.</summary>
    /// <param name="drives">Sole drive listing boundary.</param>
    /// <param name="wslRoots">Sole WSL distribution discovery boundary.</param>
    public LocationsSession(IDriveCatalog drives, IWslDistributionCatalog wslRoots)
    {
        ArgumentNullException.ThrowIfNull(drives);
        ArgumentNullException.ThrowIfNull(wslRoots);
        _drives = drives;
        _wslRoots = wslRoots;
    }

    /// <summary>Gets the current immutable Locations picker state.</summary>
    public LocationsState Current
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Opens the picker over the active pane when this scope may own input and the picker is closed,
    /// then reads both sections concurrently and decides each from its own outcome. Cancellation of
    /// either read closes the picker; a failure of one section never hides the other.
    /// </summary>
    /// <param name="panes">Pane snapshot whose active side a selection later navigates.</param>
    /// <param name="ownership">Input ownership the session derived for this scope.</param>
    /// <param name="cancellationToken">Token observed by both catalog reads.</param>
    /// <returns>The state current after the admission decision and both reads.</returns>
    public async Task<LocationsState> OpenAsync(
        DualPaneSnapshot panes,
        InteractionOwnership ownership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(ownership);
        lock (_sync)
        {
            if (ownership != InteractionOwnership.ScopeOwnsInput || _state is not LocationsClosed)
            {
                return _state;
            }
            _state = new LocationsLoading(panes.ActiveSide);
        }
        Task<DriveCatalogOutcome> drives = _drives.ListAsync(cancellationToken);
        Task<WslDistributionCatalogOutcome> wslRoots = _wslRoots.DiscoverAsync(cancellationToken);
        await Task.WhenAll(drives, wslRoots).ConfigureAwait(false);
        LocationsState settled = Settle(
            panes.ActiveSide,
            await drives.ConfigureAwait(false),
            await wslRoots.ConfigureAwait(false));
        lock (_sync)
        {
            _state = settled;
            return settled;
        }
    }

    /// <summary>
    /// Validates one intent against the open picker. Focus moves stop at both ends; a selection or
    /// cancellation qualified by a stale state changes nothing; an accepted selection closes the
    /// picker before the result names the navigation, and is refused when this scope no longer
    /// owns input. A loading or closed picker routes nothing.
    /// </summary>
    /// <param name="intent">Intent offered while this scope owns precedence.</param>
    /// <param name="ownership">Input ownership the session derived for this scope.</param>
    /// <returns>The navigation the session must perform, or nothing to route.</returns>
    public LocationsValidation Validate(UserIntent intent, InteractionOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(ownership);
        lock (_sync)
        {
            if (_state is not LocationsOpen open)
            {
                return LocationsValidation.NothingToRoute;
            }
            if (intent is LocationSelection selection)
            {
                return Select(open, selection, ownership);
            }
            if (intent is LocationsCancellation cancellation && ReferenceEquals(open, cancellation.ExpectedState))
            {
                _state = LocationsState.Closed;
            }
            else if (intent == UserIntent.MoveNext || intent == UserIntent.MovePrevious)
            {
                _state = open.MoveFocus(intent == UserIntent.MoveNext ? 1 : -1);
            }
            return LocationsValidation.NothingToRoute;
        }
    }

    private LocationsValidation Select(
        LocationsOpen open,
        LocationSelection selection,
        InteractionOwnership ownership)
    {
        if (!ReferenceEquals(open, selection.ExpectedState) || !open.Items.Contains(selection.Item))
        {
            return LocationsValidation.NothingToRoute;
        }
        _state = LocationsState.Closed;
        return ownership == InteractionOwnership.ScopeOwnsInput
            ? new LocationTargetAccepted(open.ActiveSide, selection.Item.Location)
            : LocationsValidation.NothingToRoute;
    }

    private static LocationsState Settle(
        PaneSide activeSide,
        DriveCatalogOutcome drives,
        WslDistributionCatalogOutcome wslRoots)
    {
        return drives is DriveCatalogCancelled || wslRoots is WslDistributionCatalogCancelled
            ? LocationsState.Closed
            : new LocationsOpen(activeSide, DriveSection.Of(drives), WslRootSection.Of(wslRoots), 0);
    }
}
