using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>Represents a read in flight toward one target location; intents are frozen meanwhile.</summary>
public sealed record PaneLoading : PaneActivity
{
    internal PaneLoading(FileSystemPath target)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
    }

    /// <summary>Gets the location being read.</summary>
    public FileSystemPath Target { get; }
}
