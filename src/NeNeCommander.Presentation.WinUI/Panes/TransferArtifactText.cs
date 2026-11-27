using System;
using System.Globalization;
using System.Text;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Describes historical preparation evidence without calling a partial or unidentified file a verified backup.</summary>
internal static class TransferArtifactText
{
    internal static string Compose(TransferArtifactReceipt artifact, Func<string, string> localize)
    {
        string role = localize(RoleKey(artifact.Role));
        return artifact switch
        {
            UnidentifiedTransferArtifact unidentified => string.Format(CultureInfo.InvariantCulture,
                CompositeFormat.Parse(localize("TransferArtifactAttemptedFormat")), role, unidentified.CandidatePath.CanonicalText),
            IdentifiedTransferArtifact identified => string.Format(CultureInfo.InvariantCulture,
                CompositeFormat.Parse(localize("TransferArtifactObservedFormat")), role, localize(ContentKey(identified.Content)),
                localize(LinkKey(identified.Link)), FormatPath(identified.Artifact.Path.CanonicalText, identified.Artifact.PathObservation, localize)),
            _ => throw new InvalidOperationException("The artifact observation variant cannot be displayed."),
        };
    }

    internal static string FormatPath(string path, TransferPathObservation observation, Func<string, string> localize)
    {
        return string.Format(CultureInfo.InvariantCulture, CompositeFormat.Parse(localize(
            observation == TransferPathObservation.CurrentPathVerified ? "TransferObservedCurrentPath" : "TransferObservedLastKnownPath")), path);
    }

    private static string RoleKey(TransferArtifactRole role)
    {
        return role == TransferArtifactRole.OriginalBackup ? "TransferArtifactOriginalTitle"
            : role == TransferArtifactRole.PreparedContent ? "TransferArtifactPreparedTitle" : "TransferArtifactRecoveryTitle";
    }

    private static string ContentKey(ArtifactContentObservation content)
    {
        return content switch
        {
            VerifiedArtifactContent => "TransferArtifactContentsVerified",
            RecoveryMappingContent => "TransferArtifactMappingVerified",
            _ when content == ArtifactContentObservation.Partial => "TransferArtifactContentsPartial",
            _ => "TransferArtifactContentsUnverified",
        };
    }

    private static string LinkKey(ArtifactLinkObservation link)
    {
        return link == ArtifactLinkObservation.Retained ? "TransferArtifactKept"
            : link == ArtifactLinkObservation.Removed ? "TransferArtifactRemoved" : "TransferArtifactRemovalUnconfirmed";
    }
}
