using System;
using NeNeCommander.Application.Launching;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents a user-requested file handoff in progress. Existing pane content remains unchanged.
/// </summary>
public sealed record PaneLaunching : PaneActivity
{
    private protected override void RestrictDerivation()
    {
    }

    internal PaneLaunching(FileSystemPath target, FileLaunchPurpose purpose)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(purpose);
        Target = target;
        Purpose = purpose;
    }

    /// <summary>Gets the purpose owning this external-handoff state.</summary>
    public FileLaunchPurpose Purpose { get; }

    /// <summary>Gets the validated file path being handed to its external provider.</summary>
    public FileSystemPath Target { get; }
}
