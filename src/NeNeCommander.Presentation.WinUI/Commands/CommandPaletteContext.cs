using System;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Names the captured pane context and exceptions to ordinary pane targeting.</summary>
internal sealed record CommandPaletteContext
{
    private readonly PaneSide _activeSide;

    private CommandPaletteContext(PaneSide activeSide, string heading, string target)
    {
        _activeSide = activeSide;
        Heading = heading;
        Target = target;
    }

    internal string Heading { get; }
    internal string Target { get; }

    internal static CommandPaletteContext Create(PaneSide activeSide, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(activeSide);
        ArgumentNullException.ThrowIfNull(localize);
        return new CommandPaletteContext(activeSide,
            localize(activeSide == PaneSide.Left ? "CommandPaletteContextLeft" : "CommandPaletteContextRight"),
            localize(activeSide == PaneSide.Left ? "CommandPaletteTargetLeft" : "CommandPaletteTargetRight"));
    }

    internal string? DetailFor(UserIntent intent, Func<string, string> localize)
    {
        string? resourceKey = intent switch
        {
            UserIntent value when value == UserIntent.Copy || value == UserIntent.Move =>
                _activeSide == PaneSide.Left ? "CommandPaletteTransferLeftToRight" : "CommandPaletteTransferRightToLeft",
            UserIntent value when value == UserIntent.ActivateOtherPane =>
                _activeSide == PaneSide.Left ? "CommandPaletteActivateRight" : "CommandPaletteActivateLeft",
            UserIntent value when value == UserIntent.OpenSettings => "CommandPaletteSettingsDetail",
            UserIntent value when value == UserIntent.OpenColorSchemes => "CommandPaletteThemeDetail",
            UserIntent value when value == UserIntent.OpenWindowAdjustment => "CommandPaletteWindowDetail",
            UserIntent value when value == UserIntent.OpenHelp => "CommandPaletteHelpDetail",
            UserIntent value when value == UserIntent.OpenBookmarks => "CommandPaletteBookmarksDetail",
            _ => null,
        };
        return resourceKey is null ? null : localize(resourceKey);
    }
}
