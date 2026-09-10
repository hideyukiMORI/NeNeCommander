using System;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Input;

/// <summary>Confirms raw address text against the exact editor state that owned the control.</summary>
public sealed record AddressSubmission : UserIntent
{
    internal AddressSubmission(AddressEditorState expectedState, string rawText)
    {
        ArgumentNullException.ThrowIfNull(expectedState);
        ArgumentNullException.ThrowIfNull(rawText);
        ExpectedState = expectedState;
        RawText = rawText;
    }

    /// <summary>Gets the exact editor state that owned the submitting control.</summary>
    public AddressEditorState ExpectedState { get; }

    /// <summary>Gets the unmodified text supplied by the native editor.</summary>
    public string RawText { get; }
}
