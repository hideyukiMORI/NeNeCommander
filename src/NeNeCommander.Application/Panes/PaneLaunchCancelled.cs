using System;
using NeNeCommander.Application.Launching;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents cancellation observed before a file handoff, with pane content left unchanged.
/// </summary>
public sealed record PaneLaunchCancelled : PaneActivity
{
    private protected override void RestrictDerivation()
    {
    }

    internal PaneLaunchCancelled(FileSystemPath target, FileLaunchPurpose purpose)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(purpose);
        Target = target;
        Purpose = purpose;
    }

    /// <summary>Gets the purpose owning this external-handoff state.</summary>
    public FileLaunchPurpose Purpose { get; }

    /// <summary>Gets the validated file path whose handoff was cancelled.</summary>
    public FileSystemPath Target { get; }
}
