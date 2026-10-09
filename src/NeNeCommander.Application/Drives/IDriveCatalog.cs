using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Application.Drives;

/// <summary>
/// Defines the sole boundary for listing the Windows volumes that currently have a drive-letter
/// root. Volumes are environment facts read fresh on every call; nothing is cached or persisted.
/// </summary>
public interface IDriveCatalog
{
    /// <summary>Lists a validated immutable snapshot of the current drive roots.</summary>
    /// <param name="cancellationToken">Token that cancels listing without a partial snapshot.</param>
    /// <returns>The closed listing outcome.</returns>
    public Task<DriveCatalogOutcome> ListAsync(CancellationToken cancellationToken);
}
