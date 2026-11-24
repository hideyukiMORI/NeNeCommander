using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Commands;

/// <summary>Derives a qualified context target and its surface without applying any pane or scope transition.</summary>
internal sealed record CommandContextRequest
{
    private CommandContextRequest(PaneInteraction interaction, CommandSurface surface)
    {
        Interaction = interaction;
        Surface = surface;
    }

    internal PaneInteraction Interaction { get; }

    internal CommandSurface Surface { get; }

    internal static CommandContextRequest? Create(UserIntent intent, DualPaneSnapshot panes)
    {
        PaneInteraction interaction = intent is PaneInteraction pointer
            ? pointer
            : new PaneSurfaceInteraction(panes.ActiveSide, panes.Of(panes.ActiveSide), PaneSurfaceAction.OpenContextMenu);
        if (panes.Of(interaction.Side).Content is not PaneContentListed listed)
        {
            return null;
        }
        CommandSurface surface = interaction is PaneRowInteraction ||
            (intent == UserIntent.OpenContextMenu && listed.State.FocusItem is not null)
                ? CommandSurface.ItemContext : CommandSurface.FolderContext;
        return new CommandContextRequest(interaction, surface);
    }
}
