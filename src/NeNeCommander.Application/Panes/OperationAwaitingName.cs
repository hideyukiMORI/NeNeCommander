using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents one operation waiting for the user to type a name: a directory creation whose
/// subject is the listed location, or a rename whose subject is the frozen focus item. The kind
/// and the subject are frozen when the intent arrives, and the initial name is the text the host
/// starts the editor with: empty for a creation, the entry's provider-reported name for a rename.
/// Only <see cref="Input.NameSubmission"/> and <see cref="Input.UserIntent.Escape"/> leave this state.
/// </summary>
public sealed record OperationAwaitingName : OperationActivity
{
    internal OperationAwaitingName(OperationKind kind, FileSystemPath subject, string initialName)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(initialName);
        Kind = kind;
        Subject = subject;
        InitialName = initialName;
    }

    /// <summary>Gets the kind of operation the submitted name will start.</summary>
    public OperationKind Kind { get; }

    /// <summary>Gets the frozen path the name applies to: the listed location, or the entry to rename.</summary>
    public FileSystemPath Subject { get; }

    /// <summary>Gets the text the name editor starts with.</summary>
    public string InitialName { get; }
}
