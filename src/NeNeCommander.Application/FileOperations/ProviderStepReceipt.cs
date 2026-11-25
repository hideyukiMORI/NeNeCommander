using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Owns provider effects, an admitted target observation and every preparation on all return paths.</summary>
public sealed record ProviderStepReceipt
{
    private ProviderStepReceipt(
        ImmutableList<ProviderStepEffectKind> effects,
        ProviderTargetObservation? target,
        ImmutableList<TransferArtifactReceipt> artifacts)
    {
        Effects = effects;
        Target = target;
        Artifacts = artifacts;
    }

    /// <summary>Gets the receipt when no provider effect or overwrite target was observed.</summary>
    public static ProviderStepReceipt Empty { get; } = Create([], null, []);

    /// <summary>Gets the ordered completed provider effects without mutable backing storage.</summary>
    public IReadOnlyList<ProviderStepEffectKind> Effects { get; }

    /// <summary>Gets the admitted existing overwrite target observation, or absence for an unrelated provider step.</summary>
    public ProviderTargetObservation? Target { get; }

    /// <summary>Gets every reported preparation, including removed links and unidentified creation attempts.</summary>
    public IReadOnlyList<TransferArtifactReceipt> Artifacts { get; }

    internal bool HasReportedEffects => Effects.Count != 0 || Artifacts.Count != 0 ||
        (Target is not null && Target is not UntouchedTransferTarget);

    /// <summary>Copies each input once, validates that stored snapshot, and retains target/artifact facts even on success.</summary>
    public static ProviderStepReceipt Create(
        IReadOnlyList<ProviderStepEffectKind> effects,
        ProviderTargetObservation? target,
        IReadOnlyList<TransferArtifactReceipt> artifacts)
    {
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(artifacts);
        ImmutableList<ProviderStepEffectKind> effectSnapshot = [.. effects];
        ImmutableList<TransferArtifactReceipt> artifactSnapshot = [.. artifacts];
        foreach (ProviderStepEffectKind effect in effectSnapshot)
        {
            ArgumentNullException.ThrowIfNull(effect);
        }
        ValidateArtifacts(artifactSnapshot, target);
        return new ProviderStepReceipt(effectSnapshot, target, artifactSnapshot);
    }

    private static void ValidateArtifacts(ImmutableList<TransferArtifactReceipt> artifacts, ProviderTargetObservation? target)
    {
        HashSet<TransferArtifactRole> roles = [];
        HashSet<FileObjectIdentity> objects = [];
        foreach (TransferArtifactReceipt artifact in artifacts)
        {
            ArgumentNullException.ThrowIfNull(artifact);
            if (!roles.Add(artifact.Role))
            {
                throw new ArgumentException("An artifact role must have one current observation.", nameof(artifacts));
            }
            ValidateArtifactIdentity(artifact, objects, target);
        }
        if (artifacts.Count != 0 && target is null)
        {
            throw new ArgumentException("Preparations require the admitted existing target observation.", nameof(target));
        }
    }

    private static void ValidateArtifactIdentity(
        TransferArtifactReceipt artifact,
        HashSet<FileObjectIdentity> objects,
        ProviderTargetObservation? target)
    {
        if (artifact is IdentifiedTransferArtifact identified &&
            (!objects.Add(identified.Artifact.ObjectIdentity) ||
                identified.Artifact.ObjectIdentity == target?.Target.Entry.ObjectIdentity))
        {
            throw new ArgumentException("Owned preparations must be distinct from each other and the existing target.", nameof(artifact));
        }
    }

}
