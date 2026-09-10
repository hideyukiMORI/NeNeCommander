using System;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Represents one localized, searchable, render-ready palette candidate.</summary>
public sealed record CommandPaletteRow
{
    internal CommandPaletteRow(
        CommandCandidate candidate,
        string title,
        string shortcut,
        string target,
        string opposite,
        string reason,
        string stateMarker,
        string detail,
        string automationName,
        string automationId)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        Candidate = candidate;
        Title = title;
        Shortcut = shortcut;
        Target = target;
        Opposite = opposite;
        Reason = reason;
        StateMarker = stateMarker;
        Detail = detail;
        AutomationName = automationName;
        AutomationId = automationId;
    }

    /// <summary>Gets the captured Application candidate.</summary>
    public CommandCandidate Candidate { get; }

    /// <summary>Gets the canonical intent selected by this row.</summary>
    public UserIntent Intent => Candidate.Intent;

    /// <summary>Gets whether captured state admits the command.</summary>
    public bool IsAvailable => Candidate.Availability == CommandAvailability.Available;

    /// <summary>Gets the localized command title.</summary>
    public string Title { get; }

    /// <summary>Gets the localized canonical shortcut.</summary>
    public string Shortcut { get; }

    /// <summary>Gets the localized target pane summary.</summary>
    public string Target { get; }

    /// <summary>Gets the localized opposite pane summary.</summary>
    public string Opposite { get; }

    /// <summary>Gets the localized unavailable reason, or empty text when available.</summary>
    public string Reason { get; }

    /// <summary>Gets the non-color unavailable marker, or empty text when available.</summary>
    public string StateMarker { get; }

    /// <summary>Gets the complete selected-row detail text.</summary>
    public string Detail { get; }

    /// <summary>Gets the complete localized UIA name.</summary>
    public string AutomationName { get; }

    /// <summary>Gets the stable automation identity derived from the command label identity.</summary>
    public string AutomationId { get; }
}
