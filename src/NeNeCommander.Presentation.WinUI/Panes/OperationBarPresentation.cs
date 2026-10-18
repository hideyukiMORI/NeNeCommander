using System;
using System.Collections.Generic;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Captures the operation bar's status, numeric detail, semantic tone and canonical hints.</summary>
internal sealed record OperationBarPresentation
{
    private OperationBarPresentation(
        OperationStatus status,
        OperationDetail detail,
        OperationBarTone tone,
        IReadOnlyList<KeyHint> keyHints)
    {
        Status = status;
        Detail = detail;
        Tone = tone;
        KeyHints = keyHints;
    }

    internal OperationStatus Status { get; }
    internal OperationDetail Detail { get; }
    internal OperationBarTone Tone { get; }
    internal IReadOnlyList<KeyHint> KeyHints { get; }

    internal static OperationBarPresentation Create(
        OperationStatus status,
        OperationDetail detail,
        OperationBarTone tone,
        IReadOnlyList<KeyHint> keyHints)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(detail);
        ArgumentNullException.ThrowIfNull(tone);
        ArgumentNullException.ThrowIfNull(keyHints);
        return new OperationBarPresentation(status, detail, tone, keyHints);
    }
}
