using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents a user-requested file handoff in progress. Existing pane content remains unchanged.
/// </summary>
public sealed record PaneLaunching : PaneActivity
{
    internal PaneLaunching(FileSystemPath target)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
    }

    /// <summary>Gets the validated file path being handed to its external provider.</summary>
    public FileSystemPath Target { get; }
}
