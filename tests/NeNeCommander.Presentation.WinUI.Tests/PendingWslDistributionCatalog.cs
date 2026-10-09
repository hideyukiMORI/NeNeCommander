using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Wsl;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Hand-written WSL catalog for sessions whose tests never open the Locations picker.</summary>
internal sealed class PendingWslDistributionCatalog : IWslDistributionCatalog
{
    public Task<WslDistributionCatalogOutcome> DiscoverAsync(CancellationToken cancellationToken)
    {
        return new TaskCompletionSource<WslDistributionCatalogOutcome>().Task;
    }
}
