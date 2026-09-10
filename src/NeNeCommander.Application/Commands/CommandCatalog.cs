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
        bool hasFocus = active.State.FocusItem is not null;
        bool hasSource = active.State.Selection.Count > 0 || hasFocus;
        return intent switch
        {
            _ when (intent == UserIntent.OpenFocused ||
                    intent == UserIntent.Rename ||
                    intent == UserIntent.ToggleSelection) && !hasFocus =>
                new CommandUnavailable(CommandUnavailableReason.FocusRequired),
            _ when (intent == UserIntent.Copy || intent == UserIntent.Move) && !hasSource =>
                new CommandUnavailable(CommandUnavailableReason.SourceRequired),
            _ when (intent == UserIntent.Copy || intent == UserIntent.Move) &&
                passive.Content is not PaneContentListed =>
                new CommandUnavailable(CommandUnavailableReason.PassivePaneUnavailable),
            _ when intent == UserIntent.Delete && !hasSource =>
                new CommandUnavailable(CommandUnavailableReason.SourceRequired),
            _ when intent == UserIntent.NavigateParent && active.State.Location.Parent is null =>
                new CommandUnavailable(CommandUnavailableReason.ParentUnavailable),
            _ when intent == UserIntent.NavigateBack && active.State.NavigationHistory.BackTarget is null =>
                new CommandUnavailable(CommandUnavailableReason.BackUnavailable),
            _ when intent == UserIntent.NavigateForward && active.State.NavigationHistory.ForwardTarget is null =>
                new CommandUnavailable(CommandUnavailableReason.ForwardUnavailable),
            _ => CommandAvailability.Available,
        };
    }
}
