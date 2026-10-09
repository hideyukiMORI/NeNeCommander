using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Names the one closed provider pair a transfer step runs across (ADR-0059). The pair is derived
/// from validated path variants only; text never decides it, and every pair without its own
/// variant is <see cref="Unavailable"/>.
/// </summary>
internal abstract record TransferRoute
{
    private TransferRoute()
    {
    }

    /// <summary>Derives the route shared by every frozen source of one batch.</summary>
    /// <param name="sources">Frozen batch sources.</param>
    /// <param name="destination">Validated destination location.</param>
    /// <returns>The common route, or <see cref="Unavailable"/> for an empty or mixed batch.</returns>
    internal static TransferRoute Derive(IReadOnlyList<FileEntrySnapshot> sources, FileSystemPath destination)
    {
        if (sources.Count == 0)
        {
            return new Unavailable();
        }
        TransferRoute first = Derive(sources[0].Path, destination);
        return sources.All(source => Derive(source.Path, destination) == first) ? first : new Unavailable();
    }

    /// <summary>Derives the route of one source and destination pair.</summary>
    /// <param name="source">Validated source path.</param>
    /// <param name="destination">Validated destination location.</param>
    /// <returns>The closed route of the pair.</returns>
    internal static TransferRoute Derive(FileSystemPath source, FileSystemPath destination)
    {
        return (source, destination) switch
        {
            (WindowsLocalPath, WindowsLocalPath) => new SameWindowsLocal(),
            (WslPath sourceWsl, WslPath destinationWsl) when sourceWsl.DistributionName.Equals(
                destinationWsl.DistributionName,
                StringComparison.OrdinalIgnoreCase) => new SameWslDistribution(),
            (WindowsLocalPath, WslPath) => new WindowsLocalToWsl(),
            _ => new Unavailable(),
        };
    }

    /// <summary>Both ends are Windows local paths; the Windows local adapter owns the step.</summary>
    internal sealed record SameWindowsLocal : TransferRoute;

    /// <summary>Both ends are in one WSL distribution; the WSL adapter owns the step.</summary>
    internal sealed record SameWslDistribution : TransferRoute;

    /// <summary>A Windows local source copies into a WSL distribution through the cross transfer.</summary>
    internal sealed record WindowsLocalToWsl : TransferRoute;

    /// <summary>Every other pair, an empty batch, or a mixed batch; it fails closed before any adapter.</summary>
    internal sealed record Unavailable : TransferRoute;
}
