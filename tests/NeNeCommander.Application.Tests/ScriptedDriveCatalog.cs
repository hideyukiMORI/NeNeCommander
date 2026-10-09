using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Drives;

namespace NeNeCommander.Application.Tests;

/// <summary>Hand-written drive catalog whose single pending listing completes when a test decides.</summary>
internal sealed class ScriptedDriveCatalog : IDriveCatalog
{
    private readonly TaskCompletionSource<DriveCatalogOutcome> _completion = new();

    internal int CallCount { get; private set; }

    internal CancellationToken ObservedToken { get; private set; }

    public Task<DriveCatalogOutcome> ListAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        ObservedToken = cancellationToken;
        return _completion.Task;
    }

    internal static ScriptedDriveCatalog Completed(DriveCatalogOutcome outcome)
    {
        ScriptedDriveCatalog catalog = new();
        catalog.Complete(outcome);
        return catalog;
    }

    internal void Complete(DriveCatalogOutcome outcome)
    {
        _completion.SetResult(outcome);
    }
}
