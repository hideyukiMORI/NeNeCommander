using System;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Represents one localized, searchable, render-ready palette candidate.</summary>
public sealed record CommandPaletteRow
{
    private readonly CommandPaletteCaption _caption;
    private readonly CommandPaletteContext _context;
    private readonly CommandPaletteFeedback _feedback;

    internal CommandPaletteRow(
        CommandCandidate candidate,
        CommandPaletteCaption caption,
        CommandPaletteContext context,
        CommandPaletteFeedback feedback)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(caption);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(feedback);
        Candidate = candidate;
        _caption = caption;
        _context = context;
        _feedback = feedback;
    }

    /// <summary>Gets the captured Application candidate.</summary>
    public CommandCandidate Candidate { get; }

    /// <summary>Gets the canonical intent selected by this row.</summary>
    public UserIntent Intent => Candidate.Intent;

    /// <summary>Gets whether captured state admits the command.</summary>
    public bool IsAvailable => Candidate.Availability == CommandAvailability.Available;

    /// <summary>Gets the localized command title.</summary>
    public string Title => _caption.Title;

    /// <summary>Gets the localized canonical shortcut.</summary>
    public string Shortcut => _caption.Shortcut;

    /// <summary>Gets the localized target pane summary.</summary>
    public string Target => _context.Target;

    /// <summary>Gets the localized opposite pane summary.</summary>
    public string Opposite => _context.Opposite;

    /// <summary>Gets the localized unavailable reason, or empty text when available.</summary>
    public string Reason => _feedback.Reason;

    /// <summary>Gets the non-color unavailable marker, or empty text when available.</summary>
    public string StateMarker => _feedback.StateMarker;

    /// <summary>Gets the complete selected-row detail text.</summary>
    public string Detail => _feedback.Detail;

    /// <summary>Gets the complete localized UIA name.</summary>
    public string AutomationName => _feedback.AutomationName;

    /// <summary>Gets the stable automation identity derived from the command label identity.</summary>
    public string AutomationId => _caption.AutomationId;
}
