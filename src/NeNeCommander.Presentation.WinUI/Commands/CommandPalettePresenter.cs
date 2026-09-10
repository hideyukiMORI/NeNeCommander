using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Creates localized palette rows without owning resources, catalog policy, or execution.</summary>
public static class CommandPalettePresenter
{
    /// <summary>Creates one Presentation view state for an Application-owned open scope.</summary>
    public static CommandPaletteViewState Present(
        CommandPaletteOpen open,
        Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(open);
        ArgumentNullException.ThrowIfNull(localize);
        string target = localize(open.ActiveSide == PaneSide.Left
            ? "CommandPaletteTargetLeft"
            : "CommandPaletteTargetRight");
        string opposite = localize(open.ActiveSide == PaneSide.Left
            ? "CommandPaletteOppositeRight"
            : "CommandPaletteOppositeLeft");
        IReadOnlyList<KeyBinding> bindings = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList);
        List<CommandPaletteRow> rows = [];
        foreach (CommandCandidate candidate in open.Candidates)
        {
            CommandLabel label = CommandLabelCatalog.LabelFor(candidate.Intent);
            KeyBinding binding = bindings.First(binding => binding.Intent == candidate.Intent);
            string title = localize(label.ResourceKey);
            string shortcut = localize(binding.KeyLabelResourceKey);
            string reason = Reason(candidate.Availability, localize);
            bool available = candidate.Availability == CommandAvailability.Available;
            string detail = Format(
                localize(available
                    ? "CommandPaletteAvailableDetailFormat"
                    : "CommandPaletteUnavailableDetailFormat"),
                target,
                opposite,
                reason);
            string automationName = Format(
                localize(available
                    ? "CommandPaletteAvailableAutomationNameFormat"
                    : "CommandPaletteUnavailableAutomationNameFormat"),
                title,
                shortcut,
                target,
                opposite,
                reason);
            rows.Add(new CommandPaletteRow(
                candidate,
                title,
                shortcut,
                target,
                opposite,
                reason,
                available ? string.Empty : localize("CommandPaletteUnavailableMarker"),
                detail,
                automationName,
                "CommandPaletteCandidate_" + label.ResourceKey));
        }
        return new CommandPaletteViewState(open, rows.AsReadOnly());
    }

    private static string Reason(CommandAvailability availability, Func<string, string> localize)
    {
        if (availability is not CommandUnavailable unavailable)
        {
            return string.Empty;
        }
        string resourceKey = unavailable.Reason == CommandUnavailableReason.FocusRequired
            ? "CommandUnavailableFocusRequired"
            : unavailable.Reason == CommandUnavailableReason.SourceRequired
                ? "CommandUnavailableSourceRequired"
                : unavailable.Reason == CommandUnavailableReason.PassivePaneUnavailable
                    ? "CommandUnavailablePassivePane"
                    : unavailable.Reason == CommandUnavailableReason.ParentUnavailable
                        ? "CommandUnavailableParent"
                        : unavailable.Reason == CommandUnavailableReason.BackUnavailable
                            ? "CommandUnavailableBack"
                            : unavailable.Reason == CommandUnavailableReason.ForwardUnavailable
                                ? "CommandUnavailableForward"
                                : throw new InvalidOperationException(
                                    "The command unavailable reason is not supported.");
        return localize(resourceKey);
    }

    private static string Format(string format, params object[] values)
    {
        return string.Format(CultureInfo.InvariantCulture, format, values);
    }
}
