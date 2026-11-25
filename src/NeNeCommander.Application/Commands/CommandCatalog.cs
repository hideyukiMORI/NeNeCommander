using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Commands;

/// <summary>Declares the sole stable subset of existing intents searchable by the command palette.</summary>
public static class CommandCatalog
{
    private static readonly IReadOnlyList<CommandDefinition> DeclaredCommands = Array.AsReadOnly<CommandDefinition>(
    [
        new(UserIntent.OpenFocused, CommandMenuGroup.FocusedItem),
        new(UserIntent.NavigateParent, CommandMenuGroup.PaletteOnly),
        new(UserIntent.NavigateBack, CommandMenuGroup.PaletteOnly),
        new(UserIntent.NavigateForward, CommandMenuGroup.PaletteOnly),
        new(UserIntent.Refresh, CommandMenuGroup.Folder),
        new(UserIntent.FocusAddress, CommandMenuGroup.PaletteOnly),
        new(UserIntent.Rename, CommandMenuGroup.FocusedItem),
        new(UserIntent.Copy, CommandMenuGroup.Selection),
        new(UserIntent.Move, CommandMenuGroup.Selection),
        new(UserIntent.CreateDirectory, CommandMenuGroup.New),
        new(UserIntent.CreateTextFile, CommandMenuGroup.New),
        new(UserIntent.Delete, CommandMenuGroup.Selection),
        new(UserIntent.ToggleSelection, CommandMenuGroup.PaletteOnly),
        new(UserIntent.ToggleHiddenItems, CommandMenuGroup.PaletteOnly),
        new(UserIntent.ActivateOtherPane, CommandMenuGroup.PaletteOnly),
        new(UserIntent.OpenSettings, CommandMenuGroup.PaletteOnly),
        new(UserIntent.OpenColorSchemes, CommandMenuGroup.PaletteOnly),
        new(UserIntent.SortByName, CommandMenuGroup.PaletteOnly),
        new(UserIntent.SortByExtension, CommandMenuGroup.PaletteOnly),
        new(UserIntent.OpenLocations, CommandMenuGroup.PaletteOnly),
        new(UserIntent.SortBySize, CommandMenuGroup.PaletteOnly),
        new(UserIntent.SortByModified, CommandMenuGroup.PaletteOnly),
        new(UserIntent.OpenBookmarks, CommandMenuGroup.Folder),
        new(UserIntent.OpenWindowAdjustment, CommandMenuGroup.PaletteOnly),
        new(UserIntent.OpenHelp, CommandMenuGroup.PaletteOnly),
        new(UserIntent.RevealFocused, CommandMenuGroup.FocusedItem),
        new(UserIntent.RevealLocation, CommandMenuGroup.Folder),
        new(UserIntent.OpenCurrentLocationInOtherPane, CommandMenuGroup.PaletteOnly),
        new(UserIntent.SelectAll, CommandMenuGroup.PaletteOnly),
        new(UserIntent.InvertSelection, CommandMenuGroup.PaletteOnly),
        new(UserIntent.ClearSelection, CommandMenuGroup.PaletteOnly),
    ]);

    /// <summary>Gets the searchable commands in stable empty-query order.</summary>
    public static IReadOnlyList<UserIntent> Commands { get; } =
        Array.AsReadOnly(DeclaredCommands.Select(command => command.Intent).ToArray());

    internal static IReadOnlyList<CommandCandidate> Capture(DualPaneSnapshot panes, CommandSurface surface)
    {
        PaneContentListed active = (PaneContentListed)panes.Of(panes.ActiveSide).Content;
        PaneSnapshot passive = panes.Of(panes.ActiveSide.Other);
        List<CommandCandidate> candidates = [];
        foreach (CommandDefinition command in DeclaredCommands.Where(command => command.Group.AppearsOn(surface)))
        {
            candidates.Add(new CommandCandidate(command.Intent,
                AvailabilityOf(command.Intent, active, passive), command.Group));
        }
        return candidates.AsReadOnly();
    }

    private static CommandAvailability AvailabilityOf(
        UserIntent intent,
        PaneContentListed active,
        PaneSnapshot passive)
    {
        CommandUnavailableReason? reason = UnavailableReasonOf(intent, active.State, passive);
        return reason is null ? CommandAvailability.Available : new CommandUnavailable(reason);
    }

    /// <summary>Names why a command cannot run, or null when it can.</summary>
    private static CommandUnavailableReason? UnavailableReasonOf(UserIntent intent, PaneState active, PaneSnapshot passive)
    {
        bool hasFocus = active.FocusItem is not null;
        bool hasSource = active.Selection.Count > 0 || hasFocus;
        bool transfers = intent == UserIntent.Copy || intent == UserIntent.Move;
        return intent switch
        {
            _ when (intent == UserIntent.OpenFocused || intent == UserIntent.RevealFocused ||
                    intent == UserIntent.Rename ||
                    intent == UserIntent.ToggleSelection) && !hasFocus =>
                CommandUnavailableReason.FocusRequired,
            _ when transfers && !hasSource => CommandUnavailableReason.SourceRequired,
            _ when transfers && passive.Content is not PaneContentListed =>
                CommandUnavailableReason.PassivePaneUnavailable,
            _ when intent == UserIntent.Delete && !hasSource => CommandUnavailableReason.SourceRequired,
            _ => NavigationReasonOf(intent, active),
        };
    }

    /// <summary>Names why a navigation command has no target, or null when it has one.</summary>
    private static CommandUnavailableReason? NavigationReasonOf(UserIntent intent, PaneState active)
    {
        return intent switch
        {
            _ when intent == UserIntent.NavigateParent && active.Location.Parent is null =>
                CommandUnavailableReason.ParentUnavailable,
            _ when intent == UserIntent.NavigateBack && active.NavigationHistory.BackTarget is null =>
                CommandUnavailableReason.BackUnavailable,
            _ when intent == UserIntent.NavigateForward && active.NavigationHistory.ForwardTarget is null =>
                CommandUnavailableReason.ForwardUnavailable,
            _ => null,
        };
    }
}
