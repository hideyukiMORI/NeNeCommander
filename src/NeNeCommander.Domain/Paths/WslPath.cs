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

    /// <inheritdoc />
    public override FileSystemPath? Parent =>
        LinuxPath.Length == 1 ? null : new WslPath(RemoveLastSegment(CanonicalText, RootLength), DistributionName, ParentLinuxPath);

    private int RootLength => "\\\\wsl.localhost\\".Length + DistributionName.Length + 1;

    private string ParentLinuxPath
    {
        get
        {
            int lastSeparator = LinuxPath.LastIndexOf('/');
            return lastSeparator == 0 ? "/" : LinuxPath[..lastSeparator];
        }
    }

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
