using System;
using System.Collections.Generic;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Reports separate transfer result counts without treating Skip as a filesystem effect.</summary>
public sealed record TransferResultDetail : OperationDetail
{
    private protected override void RestrictDerivation()
    {
    }

    internal TransferResultDetail(OperationKind kind, FileOperationOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(outcome);
        Kind = RequireTransferKind(kind);
        Outcome = outcome;
        NotTransferred = outcome.NotTransferred.Count;
        int deleted = 0;
        int partlyDeleted = 0;
        foreach (FileOperationEffect effect in outcome.Effects)
        {
            Copied += effect.Kind == FileOperationEffectKind.Copied ? 1 : 0;
            Verified += effect.Kind == FileOperationEffectKind.Verified ? 1 : 0;
            deleted += effect.Kind == FileOperationEffectKind.SourceDeleted ? 1 : 0;
            partlyDeleted += effect.Kind == FileOperationEffectKind.SourcePartiallyDeleted ? 1 : 0;
        }
        SourceDeletion = SourceDeletionCounts.Create(deleted, partlyDeleted);
    }

    private static OperationKind RequireTransferKind(OperationKind kind)
    {
        return kind == OperationKind.Copy || kind == OperationKind.Move ? kind
            : throw new ArgumentException("Transfer details require a copy or move operation.", nameof(kind));
    }

    internal FileOperationOutcome Outcome { get; }
    internal OperationKind Kind { get; }

    /// <summary>Gets the exact immutable provider facts retained for result disclosure.</summary>
    public IReadOnlyList<OperationProviderStep> ProviderSteps => Outcome.ProviderSteps;

    /// <summary>Gets the explicit Skip count.</summary>
    public int NotTransferred { get; }
    /// <summary>Gets the completed copy count.</summary>
    public int Copied { get; }
    /// <summary>Gets the verified target count.</summary>
    public int Verified { get; }
    /// <summary>Gets the source deletion count.</summary>
    public int SourceDeleted => SourceDeletion.Deleted;
    /// <summary>Gets the count of sources whose descendants were only partly removed.</summary>
    public int SourcePartiallyDeleted => SourceDeletion.PartiallyDeleted;
    /// <summary>Gets the separate complete and partial source-removal counts.</summary>
    public SourceDeletionCounts SourceDeletion { get; }
}
