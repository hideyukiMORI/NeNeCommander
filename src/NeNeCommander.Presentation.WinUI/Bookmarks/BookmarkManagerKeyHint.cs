using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>
/// One hint of the bookmark manager's key band (ADR-0080): the key caps from the canonical key map,
/// the localized label for the focused region, and the action a pointer press on the hint runs.
/// The movement hint only explains its keys and cannot be pressed.
/// </summary>
public sealed record BookmarkManagerKeyHint
{
    internal BookmarkManagerKeyHint(
        IReadOnlyList<string> keyLabelResourceKeys,
        string labelResourceKey,
        BookmarkManagerKeyAction action,
        string automationId)
    {
        ArgumentNullException.ThrowIfNull(keyLabelResourceKeys);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelResourceKey);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        KeyLabelResourceKeys = keyLabelResourceKeys.ToList().AsReadOnly();
        LabelResourceKey = labelResourceKey;
        Action = action;
        AutomationId = automationId;
    }

    /// <summary>Gets the key-cap resources in declaration order.</summary>
    public IReadOnlyList<string> KeyLabelResourceKeys { get; }

    /// <summary>Gets the localized label resource for the focused region.</summary>
    public string LabelResourceKey { get; }

    /// <summary>Gets the action a pointer press runs.</summary>
    public BookmarkManagerKeyAction Action { get; }

    /// <summary>Gets whether a pointer press runs the action; movement hints only explain their keys.</summary>
    public bool IsInvocable => !Action.Repeats;

    /// <summary>Gets the stable automation identifier of the hint.</summary>
    public string AutomationId { get; }
}
