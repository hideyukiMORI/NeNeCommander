using System;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Carries one window action qualified by the exact open state that owns it and by the placement
/// the host read a moment earlier. A request from an earlier mode instance is a no-op.
/// </summary>
public sealed record WindowAdjustmentRequest
{
    private WindowAdjustmentRequest(
        WindowAdjustmentOpen expectedState,
        WindowAdjustmentAction action,
        WindowPlacement placement)
    {
        ExpectedState = expectedState;
        Action = action;
        Placement = placement;
    }

    /// <summary>Gets the exact open state the caller believes owns the mode.</summary>
    public WindowAdjustmentOpen ExpectedState { get; }

    /// <summary>Gets the closed action the mode requested.</summary>
    public WindowAdjustmentAction Action { get; }

    /// <summary>Gets the placement the host read immediately before this request.</summary>
    public WindowPlacement Placement { get; }

    /// <summary>Creates one qualified window-action request.</summary>
    /// <param name="expectedState">Exact open state the caller last rendered.</param>
    /// <param name="action">Closed action the mode requested.</param>
    /// <param name="placement">Placement the host read immediately before this request.</param>
    /// <returns>A complete immutable request.</returns>
    public static WindowAdjustmentRequest Create(
        WindowAdjustmentOpen expectedState,
        WindowAdjustmentAction action,
        WindowPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(placement);
        return new WindowAdjustmentRequest(expectedState, action, placement);
    }
}
