using System;
using System.Globalization;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Reads the Linux inode, hard-link count, and change time of one entry with a read-only
/// <c>stat</c> inside the configured distribution. This is the oracle the ADR-0049 live read-only
/// cells compare the Windows-side identity against. It never mutates, is never consulted by
/// production code, and is never an identity source for the harness itself.
/// </summary>
internal sealed record LiveWslStatFact
{
    private LiveWslStatFact(long inode, long linkCount, long changeSeconds)
    {
        Inode = inode;
        LinkCount = linkCount;
        ChangeSeconds = changeSeconds;
    }

    internal long Inode { get; }

    internal long LinkCount { get; }

    internal long ChangeSeconds { get; }

    internal static LiveWslStatFact Read(WslPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Parse(LiveWslDistributionCommand.Run(
            path.DistributionName,
            ["stat", "-c", "%i %h %Z", "--", path.LinuxPath]));
    }

    private static LiveWslStatFact Parse(string standardOutput)
    {
        string[] fields = standardOutput.Split(' ');
        return fields.Length == 3 &&
            long.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out long inode) &&
            long.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out long linkCount) &&
            long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out long changeSeconds)
            ? new LiveWslStatFact(inode, linkCount, changeSeconds)
            : throw new InvalidOperationException("The read-only live stat record has an unexpected shape.");
    }
}
