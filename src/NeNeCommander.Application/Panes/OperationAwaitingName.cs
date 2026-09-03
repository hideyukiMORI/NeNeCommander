using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents a directory creation waiting for the user to name the new directory. The location
/// is frozen when the intent arrives; only <see cref="Input.NameSubmission"/> and
/// <see cref="Input.UserIntent.Escape"/> leave this state.
/// </summary>
public sealed record OperationAwaitingName : OperationActivity
{
    internal OperationAwaitingName(FileSystemPath location)
    {
        ArgumentNullException.ThrowIfNull(location);
        Location = location;
    }

    /// <summary>Gets the listed location the directory will be created in.</summary>
    public FileSystemPath Location { get; }
}
