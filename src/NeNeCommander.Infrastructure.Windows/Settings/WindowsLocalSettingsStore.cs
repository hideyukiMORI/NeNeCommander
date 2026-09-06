using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// Reads and atomically replaces the persisted settings document at one Windows local path.
/// Reads never create or repair it; writes reject detected baseline changes before publishing one
/// complete validated schema value through the provider's atomic replacement primitive.
/// </summary>
public sealed class WindowsLocalSettingsStore : ISettingsStore
{
    private const int StreamBufferSize = 4096;
    private const int SupportedSchemaVersion = 1;
    private const string TemporarySuffix = ".tmp";

    private readonly WindowsLocalPath _documentPath;
    private readonly WindowsLocalPath _temporaryPath;
    private readonly WindowsLocalIoExecutionBoundary _ioExecutionBoundary;
    private readonly ISettingsWriteTestHook _writeTestHook;
    private readonly Lock _baselineSync = new();
    private ExistingDocumentSnapshot? _expectedDocument;
    private SettingsLocationSnapshot? _expectedLocation;

    /// <summary>Initializes the store with the composed location of the settings document.</summary>
    /// <param name="documentPath">Validated Windows local path of the settings document.</param>
    /// <param name="ioExecutionBoundary">Sole scheduler for synchronous Windows filesystem work.</param>
    public WindowsLocalSettingsStore(
        WindowsLocalPath documentPath,
        WindowsLocalIoExecutionBoundary ioExecutionBoundary)
        : this(documentPath, ioExecutionBoundary, NoOpSettingsWriteTestHook.Instance)
    {
    }

    internal WindowsLocalSettingsStore(
        WindowsLocalPath documentPath,
        WindowsLocalIoExecutionBoundary ioExecutionBoundary,
        ISettingsWriteTestHook writeTestHook)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(ioExecutionBoundary);
        ArgumentNullException.ThrowIfNull(writeTestHook);
        _documentPath = documentPath;
        _temporaryPath = ParseTemporaryPath(documentPath);
        _ioExecutionBoundary = ioExecutionBoundary;
        _writeTestHook = writeTestHook;
    }

    /// <inheritdoc />
    public Task<SettingsReadOutcome> ReadAsync(CancellationToken cancellationToken)
    {
        return _ioExecutionBoundary.ExecuteAsync(() => ReadCoreAsync(cancellationToken)).Unwrap();
    }

    private async Task<SettingsReadOutcome> ReadCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            SafeLocation? location = CaptureLocation(out SettingsWriteFailureKind? locationFailure);
            if (location is null)
            {
                RememberBaselines(
                    new BlockedDocument(locationFailure!),
                    new BlockedLocation(locationFailure!));
                return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Unreadable);
            }
            RememberExpectedLocation(location);
            ReadDocumentObservation observation =
                await ReadDocumentAsync(cancellationToken).ConfigureAwait(false);
            if (!LocationMatches(location))
            {
                RememberBaselines(
                    new BlockedDocument(SettingsWriteFailureKind.UnsafeLocation),
                    new BlockedLocation(SettingsWriteFailureKind.UnsafeLocation));
                return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Unreadable);
            }
            RememberBaselines(observation.Document, location);
            return observation.Outcome;
        }
        catch (FileNotFoundException)
        {
            RememberExpectedDocument(AbsentDocument.Instance);
            return SettingsReadOutcome.Absent();
        }
        catch (DirectoryNotFoundException)
        {
            RememberExpectedDocument(AbsentDocument.Instance);
            return SettingsReadOutcome.Absent();
        }
        catch (UnauthorizedAccessException)
        {
            RememberBaselines(
                new BlockedDocument(SettingsWriteFailureKind.Unauthorized),
                new BlockedLocation(SettingsWriteFailureKind.Unauthorized));
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Unreadable);
        }
        catch (IOException)
        {
            RememberBaselines(
                new BlockedDocument(SettingsWriteFailureKind.IoFailure),
                new BlockedLocation(SettingsWriteFailureKind.IoFailure));
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Unreadable);
        }
    }

    /// <inheritdoc />
    public Task<SettingsWriteOutcome> WriteAsync(
        UserSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return _ioExecutionBoundary.ExecuteAsync(() => WriteDocument(settings, cancellationToken));
    }

    /// <summary>
    /// Opens the document for shared reading, rejects it on length before any byte is decoded,
    /// and hands the bounded text to the sole validator.
    /// </summary>
    private async Task<ReadDocumentObservation> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        FileAttributes documentAttributes = File.GetAttributes(_documentPath.CanonicalText);
        if ((documentAttributes & FileAttributes.ReparsePoint) != 0)
        {
            return new ReadDocumentObservation(
                SettingsReadOutcome.Rejected(SettingsReadFailureKind.Unreadable),
                new BlockedDocument(SettingsWriteFailureKind.ExistingDocumentRejected));
        }
        using FileStream stream = new(
            _documentPath.CanonicalText,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        if (stream.Length > SettingsDocumentValidator.MaximumDocumentLength)
        {
            return new ReadDocumentObservation(
                SettingsReadOutcome.Rejected(SettingsReadFailureKind.TooLarge),
                new BlockedDocument(SettingsWriteFailureKind.ExistingDocumentRejected));
        }

        byte[] bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
        SettingsReadOutcome outcome = SettingsDocumentValidator.Validate(Decode(bytes));
        ExistingDocumentSnapshot document = outcome is SettingsRead
            ? new PresentDocument(
                WindowsLocalEntryIdentity.Describe(new FileInfo(_documentPath.CanonicalText)),
                bytes)
            : new BlockedDocument(SettingsWriteFailureKind.ExistingDocumentRejected);
        return new ReadDocumentObservation(outcome, document);
    }

    private SettingsWriteOutcome WriteDocument(
        UserSettings settings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SettingsLocationSnapshot location = ExpectedLocationForWrite();
        if (location is BlockedLocation blockedLocation)
        {
            return RejectedBeforeTemporary(blockedLocation.Failure, SettingsDirectoryEffect.NotAttempted);
        }
        ExistingDocumentSnapshot existing = ExpectedDocumentForWrite();
        if (existing is BlockedDocument blocked)
        {
            return RejectedBeforeTemporary(blocked.Failure, SettingsDirectoryEffect.NotAttempted);
        }
        try
        {
            if (!DestinationMatches(existing))
            {
                return RejectedBeforeTemporary(
                    SettingsWriteFailureKind.DestinationChanged,
                    SettingsDirectoryEffect.NotAttempted);
            }
            if (!LocationMatches(location))
            {
                return RejectedBeforeTemporary(
                    SettingsWriteFailureKind.UnsafeLocation,
                    SettingsDirectoryEffect.NotAttempted);
            }
        }
        catch (UnauthorizedAccessException)
        {
            return RejectedBeforeTemporary(
                SettingsWriteFailureKind.Unauthorized,
                SettingsDirectoryEffect.NotAttempted);
        }
        catch (IOException)
        {
            return RejectedBeforeTemporary(
                SettingsWriteFailureKind.IoFailure,
                SettingsDirectoryEffect.NotAttempted);
        }

        string documentText = _documentPath.CanonicalText;
        string temporaryText = _temporaryPath.CanonicalText;
        SettingsDirectoryEffect directoryEffect = SettingsDirectoryEffect.NotAttempted;
        TemporaryOwnershipState temporaryOwnership = TemporaryOwnershipState.NotOwned;
        string? temporaryIdentifier = null;
        byte[] serialized = Serialize(settings);
        try
        {
            SafeLocation safeLocation = (SafeLocation)location;
            if (RequiresDirectoryCreation(safeLocation))
            {
                string directoryText = safeLocation.Directories[0].Path.CanonicalText;
                _writeTestHook.BeforeDirectoryCreation(directoryText);
                cancellationToken.ThrowIfCancellationRequested();
                directoryEffect = SettingsDirectoryEffect.CreationUnconfirmed;
                _ = Directory.CreateDirectory(directoryText);
                _writeTestHook.DirectoryCreated(directoryText);
                SafeLocation? createdLocation = CaptureLocation(
                    out SettingsWriteFailureKind? createdLocationFailure);
                if (createdLocation is null)
                {
                    return RejectedBeforeTemporary(createdLocationFailure!, directoryEffect);
                }
                if (!LocationAfterDirectoryCreationMatches(safeLocation, createdLocation))
                {
                    return RejectedBeforeTemporary(
                        SettingsWriteFailureKind.UnsafeLocation,
                        directoryEffect);
                }
                location = createdLocation;
                safeLocation = createdLocation;
                RememberExpectedLocation(location);
                directoryEffect = SettingsDirectoryEffect.CreationObserved;
            }
            else
            {
                _writeTestHook.BeforeTemporaryCreation(temporaryText);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (WindowsLocalEntryIdentity.Find(_temporaryPath) is not null)
            {
                return RejectedBeforeTemporary(
                    SettingsWriteFailureKind.TemporaryArtifactCollision,
                    directoryEffect);
            }

            using (FileStream temporary = new(
                temporaryText,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                StreamBufferSize,
                FileOptions.WriteThrough))
            {
                temporaryOwnership = TemporaryOwnershipState.Owned;
                temporary.Write(serialized);
                temporary.Flush(flushToDisk: true);
                temporaryIdentifier = WindowsFileIdentifier.DescribeHandle(temporary.SafeFileHandle);
            }
            if (temporaryIdentifier is null)
            {
                throw new InvalidOperationException("The owned temporary file has no stable identifier.");
            }
            _writeTestHook.TemporaryClosed(temporaryText);
            SafeLocation? temporaryLocation = CaptureLocation(
                out SettingsWriteFailureKind? temporaryLocationFailure);
            if (temporaryLocation is null)
            {
                return Reject(
                    temporaryLocationFailure!,
                    temporaryText,
                    temporaryOwnership,
                    temporaryIdentifier,
                    safeLocation,
                    serialized,
                    directoryEffect);
            }
            if (!LocationSnapshotsMatch(safeLocation, temporaryLocation))
            {
                return Reject(
                    SettingsWriteFailureKind.UnsafeLocation,
                    temporaryText,
                    temporaryOwnership,
                    temporaryIdentifier,
                    safeLocation,
                    serialized,
                    directoryEffect);
            }

            _writeTestHook.TemporaryFlushed(temporaryText);
            _writeTestHook.BeforePublish(documentText);
            if (!DestinationMatches(existing))
            {
                return Reject(
                    SettingsWriteFailureKind.DestinationChanged,
                    temporaryText,
                    temporaryOwnership,
                    temporaryIdentifier,
                    safeLocation,
                    serialized,
                    directoryEffect);
            }
            if (!LocationMatches(safeLocation))
            {
                return Reject(
                    SettingsWriteFailureKind.UnsafeLocation,
                    temporaryText,
                    temporaryOwnership,
                    temporaryIdentifier,
                    safeLocation,
                    serialized,
                    directoryEffect);
            }
            if (!TemporaryMatches(temporaryText, temporaryIdentifier, serialized))
            {
                return Reject(
                    SettingsWriteFailureKind.TemporaryArtifactCollision,
                    temporaryText,
                    temporaryOwnership,
                    temporaryIdentifier,
                    safeLocation,
                    serialized,
                    directoryEffect);
            }

            if (existing is PresentDocument)
            {
                File.Replace(temporaryText, documentText, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporaryText, documentText);
            }
            temporaryOwnership = TemporaryOwnershipState.NotOwned;
            _writeTestHook.Published(documentText);
            RememberPublishedLocation(safeLocation);
            RememberPublishedDocument(
                serialized,
                temporaryIdentifier);
            return SettingsWriteOutcome.Succeeded();
        }
        catch (UnauthorizedAccessException)
        {
            return Reject(
                SettingsWriteFailureKind.Unauthorized,
                temporaryText,
                temporaryOwnership,
                temporaryIdentifier,
                location,
                serialized,
                directoryEffect);
        }
        catch (IOException)
        {
            SettingsWriteFailureKind failure = temporaryOwnership == TemporaryOwnershipState.NotOwned &&
                File.Exists(temporaryText)
                ? SettingsWriteFailureKind.TemporaryArtifactCollision
                : SettingsWriteFailureKind.IoFailure;
            return Reject(
                failure,
                temporaryText,
                temporaryOwnership,
                temporaryIdentifier,
                location,
                serialized,
                directoryEffect);
        }
    }

    private ExistingDocumentSnapshot ExpectedDocumentForWrite()
    {
        lock (_baselineSync)
        {
            if (_expectedDocument is not null)
            {
                return _expectedDocument;
            }
        }

        ExistingDocumentSnapshot? capturedDocument = CaptureExistingDocument(
            out SettingsWriteOutcome? rejection);
        ExistingDocumentSnapshot captured = capturedDocument ??
            (rejection is SettingsWriteRejected rejected
                ? new BlockedDocument(rejected.Failure)
                : throw new InvalidOperationException("Settings preflight produced no closed result."));
        lock (_baselineSync)
        {
            _expectedDocument ??= captured;
            return _expectedDocument;
        }
    }

    private SettingsLocationSnapshot ExpectedLocationForWrite()
    {
        lock (_baselineSync)
        {
            if (_expectedLocation is not null)
            {
                return _expectedLocation;
            }
        }

        SafeLocation? capturedLocation = CaptureLocation(
            out SettingsWriteFailureKind? failure);
        SettingsLocationSnapshot captured = capturedLocation is not null
            ? capturedLocation
            : new BlockedLocation(failure!);
        lock (_baselineSync)
        {
            _expectedLocation ??= captured;
            return _expectedLocation;
        }
    }

    private SafeLocation? CaptureLocation(out SettingsWriteFailureKind? failure)
    {
        failure = null;
        try
        {
            List<DirectoryBaseline> directories = [];
            FileSystemPath? candidate = _documentPath.Parent;
            while (candidate is WindowsLocalPath local)
            {
                FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(local);
                if (entry is null)
                {
                    directories.Add(new AbsentDirectory(local));
                }
                else if (entry is DirectoryInfo directory &&
                    (directory.Attributes & FileAttributes.ReparsePoint) == 0)
                {
                    directories.Add(new PresentDirectory(
                        local,
                        WindowsFileIdentifier.Describe(directory.FullName)));
                }
                else
                {
                    failure = SettingsWriteFailureKind.UnsafeLocation;
                    return null;
                }
                candidate = local.Parent;
            }
            if (candidate is not null)
            {
                failure = SettingsWriteFailureKind.UnsafeLocation;
                return null;
            }
            return new SafeLocation(directories.AsReadOnly());
        }
        catch (UnauthorizedAccessException)
        {
            failure = SettingsWriteFailureKind.Unauthorized;
            return null;
        }
        catch (IOException)
        {
            failure = SettingsWriteFailureKind.IoFailure;
            return null;
        }
    }

    private static bool LocationMatches(SettingsLocationSnapshot expected)
    {
        if (expected is not SafeLocation safe)
        {
            return false;
        }
        foreach (DirectoryBaseline baseline in safe.Directories)
        {
            FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(baseline.Path);
            if (baseline is AbsentDirectory)
            {
                if (entry is not null)
                {
                    return false;
                }
                continue;
            }
            if (baseline is not PresentDirectory present ||
                entry is not DirectoryInfo directory ||
                (directory.Attributes & FileAttributes.ReparsePoint) != 0 ||
                !WindowsFileIdentifier.Describe(directory.FullName).Equals(
                    present.Identifier,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    private ExistingDocumentSnapshot? CaptureExistingDocument(
        out SettingsWriteOutcome? rejection)
    {
        rejection = null;
        try
        {
            FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(_documentPath);
            if (entry is null)
            {
                return AbsentDocument.Instance;
            }
            if (!IsRegularFile(entry))
            {
                rejection = RejectedBeforeTemporary(
                    SettingsWriteFailureKind.ExistingDocumentRejected,
                    SettingsDirectoryEffect.NotAttempted);
                return null;
            }

            using FileStream stream = new(
                _documentPath.CanonicalText,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            if (stream.Length > SettingsDocumentValidator.MaximumDocumentLength)
            {
                rejection = RejectedBeforeTemporary(
                    SettingsWriteFailureKind.ExistingDocumentRejected,
                    SettingsDirectoryEffect.NotAttempted);
                return null;
            }

            byte[] bytes = ReadExactBytes(stream);
            if (SettingsDocumentValidator.Validate(Decode(bytes)) is not SettingsRead)
            {
                rejection = RejectedBeforeTemporary(
                    SettingsWriteFailureKind.ExistingDocumentRejected,
                    SettingsDirectoryEffect.NotAttempted);
                return null;
            }

            FileIdentity identity = WindowsLocalEntryIdentity.Describe(new FileInfo(_documentPath.CanonicalText));
            return new PresentDocument(identity, bytes);
        }
        catch (UnauthorizedAccessException)
        {
            rejection = RejectedBeforeTemporary(
                SettingsWriteFailureKind.Unauthorized,
                SettingsDirectoryEffect.NotAttempted);
            return null;
        }
        catch (IOException)
        {
            rejection = RejectedBeforeTemporary(
                SettingsWriteFailureKind.IoFailure,
                SettingsDirectoryEffect.NotAttempted);
            return null;
        }
    }

    private bool DestinationMatches(ExistingDocumentSnapshot expected)
    {
        FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(_documentPath);
        if (expected is AbsentDocument)
        {
            return entry is null;
        }
        if (expected is not PresentDocument present || entry is null || !IsRegularFile(entry))
        {
            return false;
        }

        using FileStream stream = new(
            _documentPath.CanonicalText,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        if (stream.Length != present.Bytes.Length ||
            WindowsLocalEntryIdentity.Describe(new FileInfo(_documentPath.CanonicalText)) != present.Identity)
        {
            return false;
        }
        byte[] current = ReadExactBytes(stream);
        return current.AsSpan().SequenceEqual(present.Bytes);
    }

    private static byte[] ReadExactBytes(FileStream stream)
    {
        byte[] bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static string Decode(byte[] bytes)
    {
        using MemoryStream input = new(bytes, writable: false);
        using StreamReader reader = new(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private void RememberExpectedDocument(ExistingDocumentSnapshot document)
    {
        lock (_baselineSync)
        {
            _expectedDocument = document;
        }
    }

    private void RememberExpectedLocation(SettingsLocationSnapshot location)
    {
        lock (_baselineSync)
        {
            _expectedLocation = location;
        }
    }

    private void RememberBaselines(
        ExistingDocumentSnapshot document,
        SettingsLocationSnapshot location)
    {
        lock (_baselineSync)
        {
            _expectedDocument = document;
            _expectedLocation = location;
        }
    }

    private void RememberPublishedDocument(byte[] serialized, string publishedIdentifier)
    {
        ExistingDocumentSnapshot expected = UncertainDocument.Instance;
        ExistingDocumentSnapshot? captured = CaptureExistingDocument(out SettingsWriteOutcome? rejection);
        try
        {
            if (rejection is null &&
                captured is PresentDocument present &&
                present.Bytes.AsSpan().SequenceEqual(serialized) &&
                WindowsFileIdentifier.Describe(_documentPath.CanonicalText).Equals(
                    publishedIdentifier,
                    StringComparison.Ordinal))
            {
                expected = present;
            }
        }
        catch (IOException)
        {
            // The publish already completed. Keep the baseline uncertain when its identity cannot
            // be linked to the owned temporary entry instead of reporting the write as unpublished.
        }
        RememberExpectedDocument(expected);
    }

    private void RememberPublishedLocation(SafeLocation approved)
    {
        try
        {
            RememberExpectedLocation(LocationMatches(approved)
                ? approved
                : new BlockedLocation(SettingsWriteFailureKind.UnsafeLocation));
        }
        catch (UnauthorizedAccessException)
        {
            RememberExpectedLocation(new BlockedLocation(SettingsWriteFailureKind.Unauthorized));
        }
        catch (IOException)
        {
            RememberExpectedLocation(new BlockedLocation(SettingsWriteFailureKind.IoFailure));
        }
    }

    private static byte[] Serialize(UserSettings settings)
    {
        using MemoryStream output = new();
        using (Utf8JsonWriter writer = new(output))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", SupportedSchemaVersion);
            writer.WriteBoolean(
                "showHiddenItems",
                settings.HiddenItemVisibility == HiddenItemVisibility.Shown);
            writer.WriteString("colorScheme", settings.ColorScheme.Identifier);
            writer.WriteEndObject();
        }
        return output.ToArray();
    }

    private static SettingsWriteOutcome Reject(
        SettingsWriteFailureKind failure,
        string temporaryText,
        TemporaryOwnershipState temporaryOwnership,
        string? temporaryIdentifier,
        SettingsLocationSnapshot location,
        byte[] expectedTemporaryBytes,
        SettingsDirectoryEffect directoryEffect)
    {
        SettingsWriteEffect effect = SettingsWriteEffect.None;
        if (temporaryOwnership == TemporaryOwnershipState.Owned)
        {
            try
            {
                if (temporaryIdentifier is null ||
                    !LocationMatches(location) ||
                    !TemporaryMatches(temporaryText, temporaryIdentifier, expectedTemporaryBytes))
                {
                    effect = SettingsWriteEffect.TemporaryArtifactLeft;
                }
                else
                {
                    File.Delete(temporaryText);
                }
            }
            catch (UnauthorizedAccessException)
            {
                effect = SettingsWriteEffect.TemporaryArtifactLeft;
            }
            catch (IOException)
            {
                effect = SettingsWriteEffect.TemporaryArtifactLeft;
            }
        }
        return SettingsWriteOutcome.Rejected(failure, directoryEffect, effect);
    }

    private static SettingsWriteOutcome RejectedBeforeTemporary(
        SettingsWriteFailureKind failure,
        SettingsDirectoryEffect directoryEffect)
    {
        return SettingsWriteOutcome.Rejected(
            failure,
            directoryEffect,
            SettingsWriteEffect.None);
    }

    private static bool RequiresDirectoryCreation(SafeLocation location)
    {
        return location.Directories.Count > 0 && location.Directories[0] is AbsentDirectory;
    }

    private static bool LocationAfterDirectoryCreationMatches(
        SafeLocation expected,
        SafeLocation created)
    {
        if (expected.Directories.Count != created.Directories.Count)
        {
            return false;
        }
        for (int index = 0; index < expected.Directories.Count; index++)
        {
            DirectoryBaseline before = expected.Directories[index];
            DirectoryBaseline after = created.Directories[index];
            if (!FileSystemPathIdentityComparer.Instance.Equals(before.Path, after.Path) ||
                after is not PresentDirectory presentAfter)
            {
                return false;
            }
            if (before is PresentDirectory presentBefore &&
                !presentBefore.Identifier.Equals(presentAfter.Identifier, StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    private static bool LocationSnapshotsMatch(SafeLocation expected, SafeLocation current)
    {
        if (expected.Directories.Count != current.Directories.Count)
        {
            return false;
        }
        for (int index = 0; index < expected.Directories.Count; index++)
        {
            DirectoryBaseline before = expected.Directories[index];
            DirectoryBaseline after = current.Directories[index];
            if (!FileSystemPathIdentityComparer.Instance.Equals(before.Path, after.Path) ||
                before is not PresentDirectory presentBefore ||
                after is not PresentDirectory presentAfter ||
                !presentBefore.Identifier.Equals(presentAfter.Identifier, StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    private static WindowsLocalPath ParseTemporaryPath(WindowsLocalPath documentPath)
    {
        PathParseOutcome parsed = FileSystemPath.Parse(documentPath.CanonicalText + TemporarySuffix);
        return parsed is PathParseSuccess { Path: WindowsLocalPath temporary }
            ? temporary
            : throw new ArgumentException(
                "The settings document path cannot produce a valid sibling temporary path.",
                nameof(documentPath));
    }

    private static bool TemporaryMatches(
        string temporaryText,
        string expectedIdentifier,
        byte[] expectedBytes)
    {
        FileInfo temporary = new(temporaryText);
        if (!temporary.Exists ||
            (temporary.Attributes & FileAttributes.ReparsePoint) != 0 ||
            !WindowsFileIdentifier.Describe(temporaryText).Equals(
                expectedIdentifier,
                StringComparison.Ordinal))
        {
            return false;
        }
        using FileStream stream = new(
            temporaryText,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        return stream.Length == expectedBytes.Length &&
            ReadExactBytes(stream).AsSpan().SequenceEqual(expectedBytes);
    }

    private static bool IsRegularFile(FileSystemInfo entry)
    {
        return entry is FileInfo file &&
            (file.Attributes & FileAttributes.ReparsePoint) == 0;
    }

    private abstract record ExistingDocumentSnapshot;

    private sealed record AbsentDocument : ExistingDocumentSnapshot
    {
        internal static AbsentDocument Instance { get; } = new();

        private AbsentDocument()
        {
        }
    }

    private sealed record PresentDocument : ExistingDocumentSnapshot
    {
        internal PresentDocument(FileIdentity identity, byte[] bytes)
        {
            Identity = identity;
            Bytes = bytes;
        }

        internal FileIdentity Identity { get; }

        internal byte[] Bytes { get; }
    }

    private sealed record BlockedDocument : ExistingDocumentSnapshot
    {
        internal BlockedDocument(SettingsWriteFailureKind failure)
        {
            Failure = failure;
        }

        internal SettingsWriteFailureKind Failure { get; }
    }

    private sealed record UncertainDocument : ExistingDocumentSnapshot
    {
        internal static UncertainDocument Instance { get; } = new();

        private UncertainDocument()
        {
        }
    }

    private sealed record ReadDocumentObservation
    {
        internal ReadDocumentObservation(
            SettingsReadOutcome outcome,
            ExistingDocumentSnapshot document)
        {
            Outcome = outcome;
            Document = document;
        }

        internal SettingsReadOutcome Outcome { get; }

        internal ExistingDocumentSnapshot Document { get; }
    }

    private abstract record SettingsLocationSnapshot;

    private sealed record SafeLocation : SettingsLocationSnapshot
    {
        internal SafeLocation(IReadOnlyList<DirectoryBaseline> directories)
        {
            Directories = directories;
        }

        internal IReadOnlyList<DirectoryBaseline> Directories { get; }
    }

    private sealed record BlockedLocation : SettingsLocationSnapshot
    {
        internal BlockedLocation(SettingsWriteFailureKind failure)
        {
            Failure = failure;
        }

        internal SettingsWriteFailureKind Failure { get; }
    }

    private abstract record DirectoryBaseline
    {
        protected DirectoryBaseline(WindowsLocalPath path)
        {
            Path = path;
        }

        internal WindowsLocalPath Path { get; }
    }

    private sealed record AbsentDirectory : DirectoryBaseline
    {
        internal AbsentDirectory(WindowsLocalPath path)
            : base(path)
        {
        }
    }

    private sealed record PresentDirectory : DirectoryBaseline
    {
        internal PresentDirectory(WindowsLocalPath path, string identifier)
            : base(path)
        {
            Identifier = identifier;
        }

        internal string Identifier { get; }
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
