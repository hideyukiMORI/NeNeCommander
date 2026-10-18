using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Captures the localized target and opposite pane summaries for one open palette.</summary>
internal sealed record CommandPaletteContext
{
    private CommandPaletteContext(string target, string opposite)
    {
        Target = target;
        Opposite = opposite;
    }

    internal string Target { get; }
    internal string Opposite { get; }

    internal static CommandPaletteContext Create(PaneSide activeSide, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(activeSide);
        ArgumentNullException.ThrowIfNull(localize);
        return new CommandPaletteContext(
            localize(activeSide == PaneSide.Left ? "CommandPaletteTargetLeft" : "CommandPaletteTargetRight"),
            localize(activeSide == PaneSide.Left ? "CommandPaletteOppositeRight" : "CommandPaletteOppositeLeft"));
    }
}
