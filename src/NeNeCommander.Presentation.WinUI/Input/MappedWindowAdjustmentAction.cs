using System;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents a key consumed by the Presentation-owned window-adjustment mode.</summary>
public sealed record MappedWindowAdjustmentAction : KeyboardMappingOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal MappedWindowAdjustmentAction(WindowAdjustmentKeyAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Action = action;
    }

    /// <summary>Gets the window-adjustment action the key declares.</summary>
    public WindowAdjustmentKeyAction Action { get; }
}
