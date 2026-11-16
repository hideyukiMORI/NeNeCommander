using System;
using System.IO;
using System.Threading;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// Performs one admitted settings write: optional directory creation, one exclusive temporary
/// candidate, the final revalidation, and the atomic publish. It owns the attempt's temporary
/// state and removes only its own verified temporary entry when it rejects.
/// </summary>
internal sealed class SettingsWriteAttempt
{
    private const int StreamBufferSize = 4096;
    private const string TemporarySuffix = ".tmp";

    private readonly SettingsStoreScope _scope;
    private readonly byte[] _serialized;
    private readonly ExistingDocumentSnapshot _existing;
    private readonly WindowsLocalPath _temporaryPath;
    private readonly string _documentText;
    private readonly string _temporaryText;
    private readonly CancellationToken _cancellationToken;
    private SafeLocation _location;
    private SettingsDirectoryEffect _directoryEffect = SettingsDirectoryEffect.NotAttempted;
    private TemporaryOwnershipState _temporaryOwnership = TemporaryOwnershipState.NotOwned;
    private string? _temporaryIdentifier;

    private SettingsWriteAttempt(
        SettingsStoreScope scope,
        AdmittedSettingsWrite admitted,
        CancellationToken cancellationToken)
    {
        _scope = scope;
        _serialized = admitted.Serialized;
        _existing = admitted.Existing;
        _temporaryPath = admitted.TemporaryPath;
        _documentText = scope.DocumentPath.CanonicalText;
        _temporaryText = admitted.TemporaryPath.CanonicalText;
        _cancellationToken = cancellationToken;
        _location = admitted.Location;
    }

    /// <summary>
    /// Admits one write against the remembered baselines, then allocates its temporary candidate
    /// and performs it. Nothing is created before admission.
    /// </summary>
    internal static SettingsWriteOutcome Run(
        SettingsStoreScope scope,
        UserSettings settings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] serialized = SettingsDocumentSerializer.Serialize(settings);
        if (serialized.Length > SettingsDocumentValidator.MaximumDocumentLength)
        {
            return RejectedBeforeAdmission(SettingsWriteFailureKind.TooLarge);
        }
        SettingsLocationSnapshot location = scope.Baseline.LocationForWrite();
        if (location is BlockedLocation blockedLocation)
        {
            return RejectedBeforeAdmission(blockedLocation.Failure);
        }
        SafeLocation safeLocation = (SafeLocation)location;
        ExistingDocumentSnapshot existing = Admit(scope, safeLocation);
        if (existing is BlockedDocument blocked)
        {
            return RejectedBeforeAdmission(blocked.Failure);
        }

        WindowsLocalPath temporaryPath = TemporaryPath(
            scope.DocumentPath,
            scope.IdentifierSource.NextIdentifier());
        AdmittedSettingsWrite admitted = new(serialized, safeLocation, existing, temporaryPath);
        return new SettingsWriteAttempt(scope, admitted, cancellationToken).Execute();
    }

    /// <summary>Builds the attempt-scoped sibling candidate for one opaque token.</summary>
    internal static WindowsLocalPath TemporaryPath(WindowsLocalPath documentPath, Guid token)
    {
        PathParseOutcome parsed = FileSystemPath.Parse(
            documentPath.CanonicalText + "." + token.ToString("N") + TemporarySuffix);
        return parsed is PathParseSuccess { Path: WindowsLocalPath temporary }
            ? temporary
            : throw new ArgumentException(
                "The settings document path cannot produce a valid sibling temporary path.",
                nameof(documentPath));
    }

    /// <summary>
    /// Rechecks the destination and location before any temporary entry exists. A blocked result
    /// carries the rejection; any other result is the destination baseline to publish over.
    /// </summary>
    private static ExistingDocumentSnapshot Admit(SettingsStoreScope scope, SafeLocation location)
    {
        try
        {
            if (!scope.AdmitsDocument())
            {
                return new BlockedDocument(SettingsWriteFailureKind.UnsafeLocation);
            }
            ExistingDocumentSnapshot existing = scope.Baseline.DocumentForWrite();
            return existing is BlockedDocument
                ? existing
                : AdmitDestination(scope, existing, location);
        }
        catch (UnauthorizedAccessException)
        {
            return new BlockedDocument(SettingsWriteFailureKind.Unauthorized);
        }
        catch (IOException)
        {
            return new BlockedDocument(SettingsWriteFailureKind.IoFailure);
        }
    }

    private static ExistingDocumentSnapshot AdmitDestination(
        SettingsStoreScope scope,
        ExistingDocumentSnapshot existing,
        SafeLocation location)
    {
        return !scope.AdmitsDocument() ? new BlockedDocument(SettingsWriteFailureKind.UnsafeLocation)
            : !scope.Documents.Matches(existing) ? new BlockedDocument(SettingsWriteFailureKind.DestinationChanged)
            : scope.Locations.Matches(location) ? existing
            : new BlockedDocument(SettingsWriteFailureKind.UnsafeLocation);
    }

    /// <summary>
    /// Runs the attempt's steps in order. A step that returns a failure kind is rejected at once,
    /// before any later step observes or mutates anything.
    /// </summary>
    private SettingsWriteOutcome Execute()
    {
        try
        {
            SettingsWriteFailureKind? failure = PrepareDirectory() ?? AcceptTemporaryCandidate();
            if (failure is not null)
            {
                return Reject(failure);
            }
            string temporaryIdentifier = CreateTemporary();
            failure = ConfirmTemporaryLocation() ?? VerifyBeforePublish(temporaryIdentifier);
            return failure is null ? Publish(temporaryIdentifier) : Reject(failure);
        }
        catch (UnauthorizedAccessException)
        {
            return Reject(SettingsWriteFailureKind.Unauthorized);
        }
        catch (IOException)
        {
            SettingsWriteFailureKind failure = _temporaryOwnership == TemporaryOwnershipState.NotOwned &&
                _scope.AdmitsDocument() &&
                File.Exists(_temporaryText)
                ? SettingsWriteFailureKind.TemporaryArtifactCollision
                : SettingsWriteFailureKind.IoFailure;
            return Reject(failure);
        }
    }

    /// <summary>Creates the missing nearest ancestor when the approved chain requires it.</summary>
    private SettingsWriteFailureKind? PrepareDirectory()
    {
        if (!SettingsLocationObserver.RequiresDirectoryCreation(_location))
        {
            _scope.WriteTestHook.BeforeTemporaryCreation(_temporaryText);
            _cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
        string directoryText = _location.Directories[0].Path.CanonicalText;
        _scope.WriteTestHook.BeforeDirectoryCreation(directoryText);
        _cancellationToken.ThrowIfCancellationRequested();
        if (!_scope.Locations.Matches(_location))
        {
            return SettingsWriteFailureKind.UnsafeLocation;
        }
        _directoryEffect = SettingsDirectoryEffect.CreationUnconfirmed;
        _ = Directory.CreateDirectory(directoryText);
        _scope.WriteTestHook.DirectoryCreated(directoryText);
        SettingsLocationSnapshot created = _scope.Locations.Capture();
        return created is BlockedLocation blocked
            ? blocked.Failure
            : AdoptCreatedLocation((SafeLocation)created);
    }

    /// <summary>Adopts the observed created chain as the attempt's and the store's location baseline.</summary>
    private SettingsWriteFailureKind? AdoptCreatedLocation(SafeLocation created)
    {
        if (!SettingsLocationObserver.AfterDirectoryCreationMatches(_location, created))
        {
            return SettingsWriteFailureKind.UnsafeLocation;
        }
        _location = created;
        _scope.Baseline.RememberLocation(created);
        _directoryEffect = SettingsDirectoryEffect.CreationObserved;
        return null;
    }

    private SettingsWriteFailureKind? AcceptTemporaryCandidate()
    {
        return !_scope.Locations.Matches(_location) ? SettingsWriteFailureKind.UnsafeLocation
            : WindowsLocalEntryIdentity.Find(_temporaryPath) is not null ? SettingsWriteFailureKind.TemporaryArtifactCollision
            : null;
    }

    /// <summary>Exclusively creates, writes and flushes the candidate; ownership starts at creation.</summary>
    private string CreateTemporary()
    {
        using (FileStream temporary = new(
            _temporaryText,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            StreamBufferSize,
            FileOptions.WriteThrough))
        {
            _temporaryOwnership = TemporaryOwnershipState.Owned;
            temporary.Write(_serialized);
            temporary.Flush(flushToDisk: true);
            _temporaryIdentifier = WindowsFileIdentifier.DescribeHandle(temporary.SafeFileHandle);
        }
        string temporaryIdentifier = _temporaryIdentifier ??
            throw new InvalidOperationException("The owned temporary file has no stable identifier.");
        _scope.WriteTestHook.TemporaryClosed(_temporaryText);
        return temporaryIdentifier;
    }

    private SettingsWriteFailureKind? ConfirmTemporaryLocation()
    {
        SettingsLocationSnapshot current = _scope.Locations.Capture();
        return current is BlockedLocation blocked ? blocked.Failure
            : SettingsLocationObserver.SnapshotsMatch(_location, (SafeLocation)current) ? null
            : SettingsWriteFailureKind.UnsafeLocation;
    }

    private SettingsWriteFailureKind? VerifyBeforePublish(string temporaryIdentifier)
    {
        _scope.WriteTestHook.TemporaryFlushed(_temporaryText);
        _scope.WriteTestHook.BeforePublish(_documentText);
        return !_scope.AdmitsDocument() ? SettingsWriteFailureKind.UnsafeLocation
            : !_scope.Documents.Matches(_existing) ? SettingsWriteFailureKind.DestinationChanged
            : VerifyAnchoredTemporary(temporaryIdentifier);
    }

    /// <summary>The location is checked on both sides of the temporary comparison.</summary>
    private SettingsWriteFailureKind? VerifyAnchoredTemporary(string temporaryIdentifier)
    {
        return !_scope.Locations.Matches(_location) ? SettingsWriteFailureKind.UnsafeLocation
            : !_scope.Documents.TemporaryMatches(_temporaryText, temporaryIdentifier, _serialized)
                ? SettingsWriteFailureKind.TemporaryArtifactCollision
            : _scope.Locations.Matches(_location) ? null
            : SettingsWriteFailureKind.UnsafeLocation;
    }

    private SettingsWriteOutcome Publish(string temporaryIdentifier)
    {
        if (_existing is PresentDocument)
        {
            File.Replace(_temporaryText, _documentText, destinationBackupFileName: null);
        }
        else
        {
            File.Move(_temporaryText, _documentText);
        }
        _temporaryOwnership = TemporaryOwnershipState.NotOwned;
        _scope.WriteTestHook.Published(_documentText);
        _scope.Baseline.RememberPublishedLocation(_location);
        _scope.Baseline.RememberPublishedDocument(_serialized, temporaryIdentifier);
        return SettingsWriteOutcome.Succeeded();
    }

    /// <summary>Rejects with the current directory effect after cleaning up only an owned temporary.</summary>
    private SettingsWriteOutcome Reject(SettingsWriteFailureKind failure)
    {
        SettingsWriteEffect effect = _temporaryOwnership == TemporaryOwnershipState.Owned
            ? RemoveOwnedTemporary()
            : SettingsWriteEffect.None;
        return SettingsWriteOutcome.Rejected(failure, _directoryEffect, effect);
    }

    private SettingsWriteEffect RemoveOwnedTemporary()
    {
        try
        {
            if (_temporaryIdentifier is not string temporaryIdentifier ||
                !_scope.Locations.Matches(_location) ||
                !_scope.Documents.TemporaryMatches(_temporaryText, temporaryIdentifier, _serialized) ||
                !_scope.Locations.Matches(_location))
            {
                return SettingsWriteEffect.TemporaryArtifactLeft;
            }
            File.Delete(_temporaryText);
            return SettingsWriteEffect.None;
        }
        catch (UnauthorizedAccessException)
        {
            return SettingsWriteEffect.TemporaryArtifactLeft;
        }
        catch (IOException)
        {
            return SettingsWriteEffect.TemporaryArtifactLeft;
        }
    }

    private static SettingsWriteOutcome RejectedBeforeAdmission(SettingsWriteFailureKind failure)
    {
        return SettingsWriteOutcome.Rejected(
            failure,
            SettingsDirectoryEffect.NotAttempted,
            SettingsWriteEffect.None);
    }

    private abstract record TemporaryOwnershipState
    {
        internal static TemporaryOwnershipState NotOwned { get; } = new TemporaryNotOwned();

        internal static TemporaryOwnershipState Owned { get; } = new TemporaryOwned();

        private TemporaryOwnershipState()
        {
        }

        private sealed record TemporaryNotOwned : TemporaryOwnershipState;

        private sealed record TemporaryOwned : TemporaryOwnershipState;
    }
}
