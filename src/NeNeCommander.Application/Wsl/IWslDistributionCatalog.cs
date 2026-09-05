using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Application.Wsl;

/// <summary>Defines the sole boundary for discovering registered WSL distributions.</summary>
public interface IWslDistributionCatalog
{
    /// <summary>Discovers a validated immutable snapshot of registered distribution roots.</summary>
    /// <param name="cancellationToken">Token that cancels discovery without a partial snapshot.</param>
    /// <returns>The closed discovery outcome.</returns>
    public Task<WslDistributionCatalogOutcome> DiscoverAsync(CancellationToken cancellationToken);
}
