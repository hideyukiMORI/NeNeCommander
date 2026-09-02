using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Paths;

/// <summary>Represents a validated candidate contained by an exact provider-aware root.</summary>
public sealed record ContainedPath : PathContainmentOutcome
{
    internal ContainedPath(FileSystemPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        Path = path;
    }

    /// <summary>Gets the validated contained path.</summary>
    public FileSystemPath Path { get; }
}
