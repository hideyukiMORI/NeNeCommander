using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Creates one live symbolic-link fixture inside the owned run child. An unprivileged Windows
/// process cannot create a link on the WSL share, so the link is made inside the distribution with
/// one fixed argument-list <c>ln -s</c>. This is fixture setup only: the harness never reads the
/// link through this path, never mutates a product target with it, and never unlinks with it.
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
}
