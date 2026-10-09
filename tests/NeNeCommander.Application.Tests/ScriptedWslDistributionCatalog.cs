using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Wsl;

namespace NeNeCommander.Application.Tests;

/// <summary>Hand-written WSL catalog whose single pending discovery completes when a test decides.</summary>
internal sealed class ScriptedWslDistributionCatalog : IWslDistributionCatalog
{
    private readonly TaskCompletionSource<WslDistributionCatalogOutcome> _completion = new();

    internal int CallCount { get; private set; }

    internal CancellationToken ObservedToken { get; private set; }

    public Task<WslDistributionCatalogOutcome> DiscoverAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        ObservedToken = cancellationToken;
        return _completion.Task;
    }

    internal static ScriptedWslDistributionCatalog Completed(WslDistributionCatalogOutcome outcome)
    {
        ScriptedWslDistributionCatalog catalog = new();
        catalog.Complete(outcome);
        return catalog;
    }

    internal void Complete(WslDistributionCatalogOutcome outcome)
    {
        _completion.SetResult(outcome);
    }
}
