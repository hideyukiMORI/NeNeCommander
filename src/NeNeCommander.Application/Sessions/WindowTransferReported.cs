using System;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Sessions;

/// <summary>Records one exact owner's transfer observation without claiming later OS stability.</summary>
public sealed record WindowTransferReported : WindowAdjustmentOutcome
{
    internal WindowTransferReported(WindowAdjustmentAction action, WindowTransferEffect effect)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(effect);
        if (!action.ChangesDisplay)
        {
            throw new ArgumentException("Only display actions can have transfer observations.", nameof(action));
        }
        Action = action;
        Effect = effect;
    }

    /// <summary>Gets the previous/next action whose apply was observed.</summary>
    public WindowAdjustmentAction Action { get; }

    /// <summary>Gets whether no changing call began, an effect is uncertain, or its final state was observed.</summary>
    public WindowTransferEffect Effect { get; }

    private protected override void RestrictDerivation()
    {
    }
}
