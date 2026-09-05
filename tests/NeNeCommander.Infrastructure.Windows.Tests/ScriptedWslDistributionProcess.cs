using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Infrastructure.Windows.Wsl;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Provides deterministic WSL discovery process outcomes.</summary>
internal sealed class ScriptedWslDistributionProcess : IWslDistributionProcess
{
    private readonly WslDistributionProcessResult? _result;

    internal ScriptedWslDistributionProcess(WslDistributionProcessResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _result = result;
    }

    internal int InvocationCount { get; private set; }

    public Task<WslDistributionProcessResult> ListAsync(CancellationToken cancellationToken)
    {
        InvocationCount++;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_result!);
    }
}
