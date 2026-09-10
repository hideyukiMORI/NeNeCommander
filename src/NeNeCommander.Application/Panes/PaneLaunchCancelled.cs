using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents cancellation observed before a file handoff, with pane content left unchanged.
/// </summary>
public sealed record PaneLaunchCancelled : PaneActivity
{
    internal PaneLaunchCancelled(FileSystemPath target)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
    }

    /// <summary>Gets the validated file path whose handoff was cancelled.</summary>
    public FileSystemPath Target { get; }
}
