using System;
using System.Collections.Generic;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Projects palette key hints from the canonical palette binding table.</summary>
public static class CommandPaletteKeyHintPresenter
{
    /// <summary>Gets the ordered key hints declared by the canonical palette map.</summary>
    public static IReadOnlyList<CommandPaletteKeyHint> Present()
    {
        List<CommandPaletteKeyHint> hints = [];
        foreach (CommandPaletteKeyBinding binding in KeyboardIntentMapper.CommandPaletteBindings)
        {
            hints.Add(new CommandPaletteKeyHint(
                binding.Key.LabelResourceKey,
                ActionLabel(binding.Action)));
        }
        return hints.AsReadOnly();
    }

    private static string ActionLabel(CommandPaletteKeyAction action)
    {
        return action == CommandPaletteKeyAction.MovePrevious
            ? "CommandPaletteHintPrevious"
            : action == CommandPaletteKeyAction.MoveNext
                ? "CommandPaletteHintNext"
                : action == CommandPaletteKeyAction.Execute
                    ? "CommandPaletteHintExecute"
                    : action == CommandPaletteKeyAction.Cancel
                        ? "CommandPaletteHintCancel"
                        : action == CommandPaletteKeyAction.MoveFocus
                            ? "CommandPaletteHintFocus"
                            : throw new InvalidOperationException(
                                "The command palette key action is not supported.");
    }
}
