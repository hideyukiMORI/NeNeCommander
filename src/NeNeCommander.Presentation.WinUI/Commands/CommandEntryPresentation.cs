using System.Collections.Generic;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Contains immutable button content and projected ordinary-input availability.</summary>
public sealed record CommandEntryPresentation
{
    internal CommandEntryPresentation(IReadOnlyList<CommandEntryButton> buttons, CommanderSnapshot snapshot)
    {
        Buttons = buttons;
        IsEnabled = PaneInputOwnership.IsAvailable(snapshot);
    }

    /// <summary>Gets the three existing commands in their declared display order.</summary>
    public IReadOnlyList<CommandEntryButton> Buttons { get; }

    /// <summary>Gets the projected enabled appearance; Application still admits every invocation.</summary>
    public bool IsEnabled { get; }
}
