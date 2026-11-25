using System;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Input;

/// <summary>Qualifies native completion, cancellation or blur by its exact editor owner.</summary>
public sealed record PaneFilterEditSubmission : UserIntent
{
    private protected override void RestrictDerivation()
    {
    }

    internal PaneFilterEditSubmission(PaneFilterEditing expectedState, PaneFilterEditAction action, string rawText)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(rawText);
        ExpectedState = expectedState;
        Action = action;
        RawText = rawText;
    }

    /// <summary>Gets the exact rendered editor owner.</summary>
    public PaneFilterEditing ExpectedState { get; }

    /// <summary>Gets the closed exit and focus policy.</summary>
    public PaneFilterEditAction Action { get; }

    /// <summary>Gets native committed text captured at exit before a deferred TextChanged can arrive.</summary>
    public string RawText { get; }
}
