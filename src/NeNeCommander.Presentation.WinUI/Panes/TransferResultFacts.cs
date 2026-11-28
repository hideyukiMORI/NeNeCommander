using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Discloses overwrite target, source and artifact facts alongside the existing overall operation status.</summary>
public static class TransferResultFacts
{
    /// <summary>Renders only retained overwrite evidence; successful content verification never hides an overall failure or uncertain cleanup.</summary>
    public static IReadOnlyList<string> Compose(TransferResultDetail detail, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(detail);
        ArgumentNullException.ThrowIfNull(localize);
        if (!detail.ProviderSteps.Any(step => step.Receipt.Target is not null))
        {
            ImmutableList<string> empty = [];
            return empty;
        }
        List<string> rows = [];
        if (detail.Outcome.Failure is FileOperationFailureKind failure)
        {
            rows.Add(localize(FailureKey(failure)));
        }
        foreach (OperationProviderStep step in detail.ProviderSteps.Where(step => step.Receipt.Target is not null))
        {
            AddStep(rows, detail, step, localize);
        }
        if (detail.ProviderSteps.Any(step => step.Receipt.Artifacts.Any(artifact =>
            artifact is not IdentifiedTransferArtifact identified || identified.Link != ArtifactLinkObservation.Removed)))
        {
            rows.Add(localize("TransferRecoveryInspectionNotice"));
        }
        ImmutableList<string> snapshot = [.. rows];
        return snapshot;
    }

    private static void AddStep(List<string> rows, TransferResultDetail detail, OperationProviderStep step, Func<string, string> localize)
    {
        ProviderTargetObservation target = step.Receipt.Target ?? throw new InvalidOperationException("An overwrite step must retain its target.");
        string path = TransferArtifactText.FormatPath(target.Target.Entry.Path.CanonicalText, target.Target.PathObservation, localize);
        rows.Add(string.Format(CultureInfo.InvariantCulture, CompositeFormat.Parse(localize(TargetKey(target))), path));
        rows.Add(string.Format(CultureInfo.InvariantCulture, CompositeFormat.Parse(localize(
            SourceKey(detail, step.Source))), step.Source.CanonicalText));
        foreach (TransferArtifactReceipt artifact in step.Receipt.Artifacts)
        {
            rows.Add(TransferArtifactText.Compose(artifact, localize));
        }
    }

    private static string SourceKey(TransferResultDetail detail, FileSystemPath source)
    {
        IEnumerable<FileOperationEffect> effects = detail.Outcome.Effects.Where(effect =>
            FileSystemPathIdentityComparer.Instance.Equals(effect.Source, source));
        return effects switch
        {
            _ when effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted) => "TransferOverwriteSourceRemoved",
            _ when effects.Any(effect => effect.Kind == FileOperationEffectKind.SourcePartiallyDeleted) => "TransferOverwriteSourcePartlyRemoved",
            _ when detail.Kind == OperationKind.Copy || !effects.Any(effect => effect.Kind == FileOperationEffectKind.Verified)
                => "TransferOverwriteSourceRetained",
            _ => "TransferOverwriteSourceUnconfirmed",
        };
    }

    private static string TargetKey(ProviderTargetObservation target)
    {
        return target switch
        {
            UntouchedTransferTarget => "TransferTargetUntouched",
            OriginalTargetContentVerified => "TransferTargetOriginalVerified",
            TargetMayHaveChanged => "TransferTargetMayHaveChanged",
            DesiredTargetContentVerified => "TransferTargetDesiredVerified",
            _ => throw new InvalidOperationException("The target observation variant cannot be displayed."),
        };
    }

    private static string FailureKey(FileOperationFailureKind failure)
    {
        return failure switch
        {
            _ when failure == FileOperationFailureKind.Copy => "TransferFailureCopy",
            _ when failure == FileOperationFailureKind.Move => "TransferFailureMove",
            _ when failure == FileOperationFailureKind.Verification => "TransferFailureVerification",
            _ when failure == FileOperationFailureKind.Delete => "TransferFailureDelete",
            _ when failure == FileOperationFailureKind.Conflict => "TransferFailureConflict",
            _ when failure == FileOperationFailureKind.IdentityChanged => "TransferFailureIdentityChanged",
            _ => AdmissionFailureKey(failure),
        };
    }

    private static string AdmissionFailureKey(FileOperationFailureKind failure)
    {
        return failure switch
        {
            _ when failure == FileOperationFailureKind.Reentrant => "TransferFailureReentrant",
            _ when failure == FileOperationFailureKind.Inspection => "TransferFailureInspection",
            _ when failure == FileOperationFailureKind.ConfirmationRequired => "TransferFailureConfirmation",
            _ when failure == FileOperationFailureKind.ProviderUnavailable => "TransferFailureUnavailable",
            _ when failure == FileOperationFailureKind.AccessDenied => "TransferFailureAccessDenied",
            _ when failure == FileOperationFailureKind.NotFound => "TransferFailureNotFound",
            _ => throw new InvalidOperationException("The transfer failure variant cannot be displayed."),
        };
    }
}
