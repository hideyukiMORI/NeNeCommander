using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Owns the exact ordered effects and explicitly skipped sources reported by an outcome.</summary>
internal sealed record FileOperationEffectSummary
{
    private readonly ReadOnlyCollection<FileOperationEffect> _effects;
    private readonly ReadOnlyCollection<FileSystemPath> _notTransferred;

    private FileOperationEffectSummary(ReadOnlyCollection<FileOperationEffect> effects, ReadOnlyCollection<FileSystemPath> notTransferred)
    {
        _effects = effects;
        _notTransferred = notTransferred;
    }

    internal IReadOnlyList<FileOperationEffect> Effects => _effects;
    internal IReadOnlyList<FileSystemPath> NotTransferred => _notTransferred;

    internal static FileOperationEffectSummary Create(IReadOnlyList<FileOperationEffect> effects, IReadOnlyList<FileSystemPath> notTransferred)
    {
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(notTransferred);
        return new FileOperationEffectSummary(new List<FileOperationEffect>(effects).AsReadOnly(), new List<FileSystemPath>(notTransferred).AsReadOnly());
    }
}
