using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>Represents a read stopped by cancellation that left the content unchanged.</summary>
public sealed record PaneReadCancelled : PaneActivity
{
    internal PaneReadCancelled(FileSystemPath target)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
    }

    /// <summary>Gets the location whose read was cancelled.</summary>
    public FileSystemPath Target { get; }
}
