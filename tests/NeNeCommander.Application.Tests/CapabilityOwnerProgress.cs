using System.Collections.Generic;
using System.Threading;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Captures progress values and the operation-owner context that publishes them.</summary>
internal sealed class CapabilityOwnerProgress : IFileOperationProgressObserver
{
    private readonly List<(FileOperationProgress Progress, SynchronizationContext? Context)> _reports = [];

    internal IReadOnlyList<(FileOperationProgress Progress, SynchronizationContext? Context)> Reports => _reports.AsReadOnly();

    public void Report(FileOperationProgress progress)
    {
        _reports.Add((progress, SynchronizationContext.Current));
    }
}
