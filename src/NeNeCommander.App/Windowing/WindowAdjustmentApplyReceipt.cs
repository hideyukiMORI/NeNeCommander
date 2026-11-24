using System;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.App.Windowing;

/// <summary>Binds the last rendered owner to exactly one synchronous decision during native callbacks.</summary>
internal sealed record WindowAdjustmentApplyReceipt
{
    internal WindowAdjustmentApplyReceipt(WindowAdjustmentOpen incoming, WindowAdjustmentPlanned decision)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        ArgumentNullException.ThrowIfNull(decision);
        Incoming = incoming;
        Decision = decision;
    }

    internal WindowAdjustmentOpen Incoming { get; }

    internal WindowAdjustmentPlanned Decision { get; }
}
