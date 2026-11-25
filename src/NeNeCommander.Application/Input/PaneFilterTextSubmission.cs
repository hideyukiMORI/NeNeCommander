using System;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Input;

/// <summary>Carries native committed text qualified by its exact filter editor owner.</summary>
public sealed record PaneFilterTextSubmission : UserIntent
{
    private protected override void RestrictDerivation()
    {
    }

    internal PaneFilterTextSubmission(PaneFilterEditing expectedState, string rawText)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ArgumentNullException.ThrowIfNull(rawText);
        ExpectedState = expectedState;
        RawText = rawText;
    }

    /// <summary>Gets the exact rendered editor owner.</summary>
    public PaneFilterEditing ExpectedState { get; }

    /// <summary>Gets the exact committed text, parsed only after ownership admission.</summary>
    public string RawText { get; }
}
