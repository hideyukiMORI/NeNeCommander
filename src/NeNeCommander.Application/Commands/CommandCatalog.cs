using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Commands;

/// <summary>Declares the sole stable subset of existing intents searchable by the command palette.</summary>
public static class CommandCatalog
{
    private static readonly IReadOnlyList<UserIntent> DeclaredCommands = Array.AsReadOnly<UserIntent>(
    [
        UserIntent.OpenFocused,
        UserIntent.NavigateParent,
        UserIntent.NavigateBack,
        UserIntent.NavigateForward,
        UserIntent.Refresh,
        UserIntent.FocusAddress,
        UserIntent.Rename,
        UserIntent.Copy,
        UserIntent.Move,
        UserIntent.CreateDirectory,
        UserIntent.Delete,
        UserIntent.ToggleSelection,
        UserIntent.ToggleHiddenItems,
        UserIntent.ActivateOtherPane,
        UserIntent.OpenSettings,
        UserIntent.SortByName,
        UserIntent.SortByExtension,
        UserIntent.OpenLocations,
        UserIntent.SortBySize,
        UserIntent.SortByModified,
        UserIntent.OpenBookmarks,
        UserIntent.OpenWindowAdjustment,
        UserIntent.OpenHelp,
    ]);

    /// <summary>Gets the searchable commands in stable empty-query order.</summary>
    public static IReadOnlyList<UserIntent> Commands { get; } = DeclaredCommands;

    internal static bool Contains(UserIntent intent)
    {
        return DeclaredCommands.Contains(intent);
    }

    internal static IReadOnlyList<CommandCandidate> Capture(DualPaneSnapshot panes)
    {
        PaneContentListed active = (PaneContentListed)panes.Of(panes.ActiveSide).Content;
        PaneSnapshot passive = panes.Of(panes.ActiveSide.Other);
        List<CommandCandidate> candidates = [];
        foreach (UserIntent intent in DeclaredCommands)
        {
            candidates.Add(new CommandCandidate(intent, AvailabilityOf(intent, active, passive)));
        }
        return candidates.AsReadOnly();
    }

    private static CommandAvailability AvailabilityOf(
        UserIntent intent,
        PaneContentListed active,
        PaneSnapshot passive)
    {
        CommandUnavailableReason? reason = EntryReasonOf(intent, active.State, passive) ??
            NavigationReasonOf(intent, active.State);
        return reason is null ? CommandAvailability.Available : new CommandUnavailable(reason);
    }

    /// <summary>Names why a command that acts on entries cannot run, or null when it can.</summary>
    private static CommandUnavailableReason? EntryReasonOf(UserIntent intent, PaneState active, PaneSnapshot passive)
    {
        bool hasFocus = active.FocusItem is not null;
        bool hasSource = active.Selection.Count > 0 || hasFocus;
        bool transfers = intent == UserIntent.Copy || intent == UserIntent.Move;
        return intent switch
        {
            _ when (intent == UserIntent.OpenFocused ||
                    intent == UserIntent.Rename ||
                    intent == UserIntent.ToggleSelection) && !hasFocus =>
                CommandUnavailableReason.FocusRequired,
            _ when transfers && !hasSource => CommandUnavailableReason.SourceRequired,
            _ when transfers && passive.Content is not PaneContentListed =>
                CommandUnavailableReason.PassivePaneUnavailable,
            _ when intent == UserIntent.Delete && !hasSource => CommandUnavailableReason.SourceRequired,
            _ => null,
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
