using System;
using System.Globalization;
using System.Text;
using NeNeCommander.Application.Commands;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Formats availability explanation and accessible text for one captured candidate.</summary>
internal sealed record CommandPaletteFeedback
{
    private static readonly CompositeFormat AvailableDetailFormat = CompositeFormat.Parse("{0} · {1}");
    private static readonly CompositeFormat AvailableAutomationNameFormat = CompositeFormat.Parse("{0}; {1}; {2}; {3}");

    private CommandPaletteFeedback(string reason, string stateMarker, string detail, string automationName)
    {
        Reason = reason;
        StateMarker = stateMarker;
        Detail = detail;
        AutomationName = automationName;
    }

    internal string Reason { get; }
    internal string StateMarker { get; }
    internal string Detail { get; }
    internal string AutomationName { get; }

    internal static CommandPaletteFeedback Create(
        CommandAvailability availability,
        CommandPaletteCaption caption,
        CommandPaletteContext context,
        Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(availability);
        ArgumentNullException.ThrowIfNull(caption);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(localize);
        string reason = availability is CommandUnavailable unavailable
            ? localize(ReasonResourceKey(unavailable.Reason))
            : string.Empty;
        bool available = availability == CommandAvailability.Available;
        string detail = available
            ? string.Format(CultureInfo.InvariantCulture, AvailableDetailFormat, context.Target, context.Opposite)
            : Format(localize("CommandPaletteUnavailableDetailFormat"), context.Target, context.Opposite, reason);
        string automationName = available
            ? string.Format(CultureInfo.InvariantCulture, AvailableAutomationNameFormat,
                caption.Title, caption.Shortcut, context.Target, context.Opposite)
            : Format(localize("CommandPaletteUnavailableAutomationNameFormat"),
                caption.Title, caption.Shortcut, context.Target, context.Opposite, reason);
        return new CommandPaletteFeedback(reason,
            available ? string.Empty : localize("CommandPaletteUnavailableMarker"), detail, automationName);
    }

    private static string ReasonResourceKey(CommandUnavailableReason reason)
    {
        return reason switch
        {
            CommandUnavailableReason value when value == CommandUnavailableReason.FocusRequired => "CommandUnavailableFocusRequired",
            CommandUnavailableReason value when value == CommandUnavailableReason.SourceRequired => "CommandUnavailableSourceRequired",
            CommandUnavailableReason value when value == CommandUnavailableReason.PassivePaneUnavailable => "CommandUnavailablePassivePane",
            CommandUnavailableReason value when value == CommandUnavailableReason.ParentUnavailable => "CommandUnavailableParent",
            CommandUnavailableReason value when value == CommandUnavailableReason.BackUnavailable => "CommandUnavailableBack",
            CommandUnavailableReason value when value == CommandUnavailableReason.ForwardUnavailable => "CommandUnavailableForward",
            _ => throw new InvalidOperationException("The command unavailable reason is not supported."),
        };
    }

    private static string Format(string format, params object[] values)
    {
        return string.Format(CultureInfo.InvariantCulture, format, values);
    }
}
