using System;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Maps WSL paths of any distribution onto an extended-length directory of a test-owned NTFS root.
/// Every mapping starts from the production <see cref="WslWin32PathText"/> output and only
/// replaces its distribution prefix, so every segment a test observes is the segment the
/// production resolver produced (ADR-0060).
/// </summary>
internal static class OwnedWslNamespace
{
    private const string OperationRoot = "\\owned";

    /// <summary>Maps the WSL tree <c>/owned</c> onto <paramref name="verbatimRoot"/>.</summary>
    internal static string Resolve(string verbatimRoot, WslPath path)
    {
        ArgumentNullException.ThrowIfNull(verbatimRoot);
        string remainder = Remainder(path);
        return remainder.Equals(OperationRoot, StringComparison.Ordinal)
            ? verbatimRoot
            : remainder.StartsWith(OperationRoot + "\\", StringComparison.Ordinal)
                ? verbatimRoot + remainder[OperationRoot.Length..]
                : throw new InvalidOperationException("The mapped WSL path is outside the test-owned root.");
    }

    /// <summary>Maps a whole distribution, from its root down, onto <paramref name="verbatimRoot"/>.</summary>
    internal static string ResolveDistribution(string verbatimRoot, WslPath path)
    {
        ArgumentNullException.ThrowIfNull(verbatimRoot);
        string remainder = Remainder(path);
        return remainder.Equals("\\", StringComparison.Ordinal) ? verbatimRoot : verbatimRoot + remainder;
    }

    private static string Remainder(WslPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        string resolved = WslWin32PathText.Resolve(path);
        string distributionPrefix = "\\\\?\\UNC\\wsl.localhost\\" + path.DistributionName;
        return resolved.StartsWith(distributionPrefix, StringComparison.Ordinal)
            ? resolved[distributionPrefix.Length..]
            : throw new InvalidOperationException("The resolver produced text outside its distribution.");
    }
}
