using System;
using NeNeCommander.Application.Launching;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>Represents a normalized file handoff failure with pane content left unchanged.</summary>
public sealed record PaneLaunchFailed : PaneActivity
{
    internal PaneLaunchFailed(FileSystemPath target, FileLaunchFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(failure);
        Target = target;
        Failure = failure;
    }

    /// <summary>Gets the validated file path whose handoff failed.</summary>
    public FileSystemPath Target { get; }

    /// <summary>Gets the normalized reason the provider did not accept the handoff.</summary>
    public FileLaunchFailureKind Failure { get; }
}
