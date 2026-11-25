using System.Collections.Generic;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Accumulates gateway effects and exact provider receipts for one execution lifetime.</summary>
internal sealed class FileOperationEffectJournal
{
    private readonly List<FileOperationEffect> _effects = [];
    private readonly List<OperationProviderStep> _providerSteps = [];

    internal int Count => _effects.Count;

    internal void Add(FileOperationEffect effect)
    {
        _effects.Add(effect);
    }

    internal void RecordProvider(FileSystemPath source, ProviderStepReceipt receipt)
    {
        _providerSteps.Add(OperationProviderStep.Create(source, receipt));
    }

    internal FileOperationEffectSummary Snapshot(IReadOnlyList<FileSystemPath> notTransferred)
    {
        return FileOperationEffectSummary.Create(_effects, notTransferred, _providerSteps);
    }
}
