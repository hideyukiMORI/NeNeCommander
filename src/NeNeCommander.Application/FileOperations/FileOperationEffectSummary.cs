using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Owns gateway effects, explicit skips and all provider receipts in one immutable outcome snapshot.</summary>
internal sealed record FileOperationEffectSummary
{
    private FileOperationEffectSummary(
        ImmutableList<FileOperationEffect> effects,
        ImmutableList<FileSystemPath> notTransferred,
        ImmutableList<OperationProviderStep> providerSteps)
    {
        Effects = effects;
        NotTransferred = notTransferred;
        ProviderSteps = providerSteps;
    }

    internal static FileOperationEffectSummary Empty { get; } = Create([], [], []);
    internal IReadOnlyList<FileOperationEffect> Effects { get; }
    internal IReadOnlyList<FileSystemPath> NotTransferred { get; }
    internal IReadOnlyList<OperationProviderStep> ProviderSteps { get; }
    internal bool HasEffects => Effects.Count != 0 || ProviderSteps.Any(step => step.Receipt.HasReportedEffects);

    internal static FileOperationEffectSummary Create(
        IReadOnlyList<FileOperationEffect> effects,
        IReadOnlyList<FileSystemPath> notTransferred,
        IReadOnlyList<OperationProviderStep> providerSteps)
    {
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(notTransferred);
        ArgumentNullException.ThrowIfNull(providerSteps);
        ImmutableList<FileOperationEffect> effectSnapshot = [.. effects];
        ImmutableList<FileSystemPath> skippedSnapshot = [.. notTransferred];
        ImmutableList<OperationProviderStep> stepSnapshot = [.. providerSteps];
        foreach (FileOperationEffect effect in effectSnapshot)
        {
            ArgumentNullException.ThrowIfNull(effect);
        }
        foreach (FileSystemPath source in skippedSnapshot)
        {
            ArgumentNullException.ThrowIfNull(source);
        }
        foreach (OperationProviderStep step in stepSnapshot)
        {
            ArgumentNullException.ThrowIfNull(step);
        }
        return new FileOperationEffectSummary(
            effectSnapshot,
            skippedSnapshot,
            stepSnapshot);
    }
}
