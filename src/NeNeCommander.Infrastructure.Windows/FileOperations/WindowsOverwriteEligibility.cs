using System;
using System.IO;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Reads initial capability through the same pair lease used by effect admission; no file or token is created.</summary>
internal static class WindowsOverwriteEligibility
{
    internal static TransferOverwriteCapability Inspect(FileEntrySnapshot source, FileSystemPath target, WindowsDriveBoundary driveBoundary)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(driveBoundary);
        return source.Path is not WindowsLocalPath || target is not WindowsLocalPath localTarget
            ? TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.UnsupportedLocation)
            : FileSystemPathIdentityComparer.Instance.Equals(source.Path, target)
                ? TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.SameObject)
                : InspectPair(source, localTarget, driveBoundary);
    }

    private static TransferOverwriteCapability InspectPair(FileEntrySnapshot source, WindowsLocalPath target, WindowsDriveBoundary driveBoundary)
    {
        try
        {
            WindowsTransferRecoveryFrame.RequireText(source.Path.CanonicalText);
            WindowsTransferRecoveryFrame.RequireText(target.CanonicalText);
            using WindowsOverwritePairLease pair = WindowsOverwritePairLease.Open(source, target, driveBoundary);
            WindowsFileIdentifier.RequireCaseInsensitiveDirectory(pair.Target.ParentHandle);
            WindowsTransferArtifactNames.RequireCandidateShape(target);
            pair.Revalidate(pair.Evidence);
            return TransferOverwriteCapability.Available(pair.Evidence);
        }
        catch (WindowsOverwriteRefusalException exception)
        {
            return TransferOverwriteCapability.Unavailable(exception.Reason);
        }
        catch (InvalidDataException)
        {
            return TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.Unconfirmed);
        }
        catch (IOException)
        {
            return TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.Unconfirmed);
        }
        catch (UnauthorizedAccessException)
        {
            return TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.Unconfirmed);
        }
        catch (ArgumentException)
        {
            return TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.Unconfirmed);
        }
        catch (NotSupportedException)
        {
            return TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.Unconfirmed);
        }
    }
}
