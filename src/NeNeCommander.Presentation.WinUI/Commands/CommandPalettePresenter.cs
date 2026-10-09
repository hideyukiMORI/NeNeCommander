using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>
/// Creates localized palette rows without owning resources, catalog policy, or execution. The
/// separators that join already localized parts are palette design rather than language, so they
/// are literal formats here; a format that carries its own words stays a localized resource.
/// </summary>
public static class CommandPalettePresenter
{
    private static readonly CompositeFormat AvailableDetailFormat = CompositeFormat.Parse("{0} · {1}");
    private static readonly CompositeFormat AvailableAutomationNameFormat = CompositeFormat.Parse("{0}; {1}; {2}; {3}");

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
            string detail = available
                ? string.Format(CultureInfo.InvariantCulture, AvailableDetailFormat, target, opposite)
                : Format(localize("CommandPaletteUnavailableDetailFormat"), target, opposite, reason);
            string automationName = available
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    AvailableAutomationNameFormat,
                    title,
                    shortcut,
                    target,
                    opposite)
                : Format(
                    localize("CommandPaletteUnavailableAutomationNameFormat"),
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
