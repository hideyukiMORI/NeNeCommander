using System;

namespace NeNeCommander.Domain.Paths;

/// <summary>
/// Represents a validated path inside one WSL distribution.
/// </summary>
public sealed record WslPath : FileSystemPath
{
    internal WslPath(string canonicalText, string distributionName, string linuxPath)
        : base(canonicalText)
    {
        DistributionName = distributionName;
        LinuxPath = linuxPath;
    }

    /// <summary>Gets the case-preserving registered distribution name.</summary>
    public string DistributionName { get; }

    /// <summary>Gets the normalized absolute Linux path.</summary>
    public string LinuxPath { get; }

    internal override bool HasSameIdentity(FileSystemPath other)
    {
        return other is WslPath wsl &&
            StringComparer.OrdinalIgnoreCase.Equals(DistributionName, wsl.DistributionName) &&
            StringComparer.Ordinal.Equals(LinuxPath, wsl.LinuxPath);
    }

    internal override int GetIdentityHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(DistributionName),
            StringComparer.Ordinal.GetHashCode(LinuxPath));
    }
}
