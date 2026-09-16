using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Creates and removes one live symbolic-link fixture inside the owned run child. An unprivileged
/// Windows process can neither create nor unlink a Linux symlink through 9P, so both operations
/// run inside the distribution with one fixed argument-list command. They are fixture setup and
/// fixture teardown only: the harness reads the link solely through
/// <see cref="NeNeCommander.Infrastructure.Windows.FileOperations.WindowsWslFileSystem"/> without
/// following it, and no product path is involved.
/// </summary>
internal static class LiveWslLinkFixture
{
    internal static void Create(WslPath target, WslPath link)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(link);
        if (!target.DistributionName.Equals(link.DistributionName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The live link fixture crosses distributions.");
        }
        _ = LiveWslDistributionCommand.Run(
            link.DistributionName,
            ["ln", "-s", "--", target.LinuxPath, link.LinuxPath]);
    }

    /// <summary>Removes one link entry. <c>unlink</c> never follows the link it removes.</summary>
    /// <param name="link">Validated path of the link entry inside the owned run child.</param>
    internal static void Unlink(WslPath link)
    {
        ArgumentNullException.ThrowIfNull(link);
        _ = LiveWslDistributionCommand.Run(link.DistributionName, ["unlink", "--", link.LinuxPath]);
    }
}
