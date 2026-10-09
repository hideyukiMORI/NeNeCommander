using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents a read the user abandoned while it was in flight. The content, focus, selection,
/// and history the pane showed before the read are unchanged, the read's token was cancelled, and
/// any result it returns later is discarded. It is not a provider failure, and every intent is
/// accepted again (ADR-0058).
/// </summary>
public sealed record PaneReadAbandoned : PaneActivity
{
    internal PaneReadAbandoned(FileSystemPath target)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
    }

    /// <summary>Gets the location whose read was abandoned.</summary>
    public FileSystemPath Target { get; }
}
