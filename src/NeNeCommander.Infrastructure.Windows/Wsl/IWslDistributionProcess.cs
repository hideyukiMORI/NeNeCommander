using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Infrastructure.Windows.Wsl;

/// <summary>Owns the one fixed WSL distribution-list process invocation.</summary>
internal interface IWslDistributionProcess
{
    /// <summary>Runs the fixed list invocation and returns its bounded output.</summary>
    internal Task<WslDistributionProcessResult> ListAsync(CancellationToken cancellationToken);
}
