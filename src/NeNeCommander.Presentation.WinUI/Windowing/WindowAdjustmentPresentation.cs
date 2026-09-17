using System;
using System.Collections.Generic;

namespace NeNeCommander.Presentation.WinUI.Windowing;

/// <summary>
/// Represents everything the window-adjustment helper renders: its localized title, the localized
/// text of the most recent outcome, the semantic tone of that text, and the hints generated from
/// the mode's own key map. The three visible helper states differ only in the outcome text and its
/// tone, and the tone is never the only carrier of a refusal.
/// </summary>
public sealed record WindowAdjustmentPresentation
{
    internal WindowAdjustmentPresentation(
        string titleResourceKey,
        string outcomeResourceKey,
        string outcomeBrushResourceKey,
        IReadOnlyList<WindowAdjustmentKeyHint> keyHints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titleResourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcomeResourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcomeBrushResourceKey);
        ArgumentNullException.ThrowIfNull(keyHints);
        TitleResourceKey = titleResourceKey;
        OutcomeResourceKey = outcomeResourceKey;
        OutcomeBrushResourceKey = outcomeBrushResourceKey;
        KeyHints = keyHints;
    }

    /// <summary>Gets the localization resource of the mode title, which is also the helper's UIA name.</summary>
    public string TitleResourceKey { get; }

    /// <summary>Gets the localization resource of the most recent outcome, which is also the helper's UIA help text.</summary>
    public string OutcomeResourceKey { get; }

    /// <summary>Gets the semantic brush resource of the outcome text.</summary>
    public string OutcomeBrushResourceKey { get; }

    /// <summary>Gets the hints the helper shows, one per declared hint group.</summary>
    public IReadOnlyList<WindowAdjustmentKeyHint> KeyHints { get; }
}
