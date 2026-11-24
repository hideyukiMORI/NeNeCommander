using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Captures one command's localized caption and stable label-derived identity.</summary>
internal sealed record CommandPaletteCaption
{
    private CommandPaletteCaption(string title, string shortcut, string automationId)
    {
        Title = title;
        Shortcut = shortcut;
        AutomationId = automationId;
    }

    internal string Title { get; }
    internal string Shortcut { get; }
    internal string AutomationId { get; }

    internal static CommandPaletteCaption Create(
        UserIntent intent,
        IReadOnlyList<KeyBinding> bindings,
        Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(localize);
        CommandLabel label = CommandLabelCatalog.LabelFor(intent);
        KeyBinding? binding = bindings.FirstOrDefault(binding => binding.Intent == intent);
        string shortcut = binding is null ? string.Empty : localize(binding.KeyLabelResourceKey);
        return new CommandPaletteCaption(
            localize(label.ResourceKey),
            shortcut,
            "CommandPaletteCandidate_" + label.ResourceKey);
    }
}
