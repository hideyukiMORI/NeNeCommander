using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// Observes the settings document and one attempt's temporary entry by identity and exact bytes.
/// Every observation re-admits the document path through the drive boundary between probes.
/// </summary>
internal sealed class SettingsDocumentObserver
{
    private readonly WindowsLocalPath _documentPath;
    private readonly WindowsDriveBoundary _driveBoundary;

    internal SettingsDocumentObserver(WindowsLocalPath documentPath, WindowsDriveBoundary driveBoundary)
    {
        _documentPath = documentPath;
        _driveBoundary = driveBoundary;
    }

    /// <summary>
    /// Opens the document for shared reading, rejects it on length before parsing, and hands the
    /// bounded bytes to the sole validator.
    /// </summary>
    internal async Task<ReadDocumentObservation> ReadAsync(CancellationToken cancellationToken)
    {
        if (!AdmitsDocument())
        {
            return UnsafeReadDocument();
        }
        FileAttributes documentAttributes = File.GetAttributes(_documentPath.CanonicalText);
        if ((documentAttributes & FileAttributes.ReparsePoint) != 0)
        {
            return RejectedReadDocument(SettingsReadFailureKind.Unreadable);
        }
        using FileStream stream = OpenRead(_documentPath.CanonicalText);
        if (stream.Length > SettingsDocumentValidator.MaximumDocumentLength)
        {
            return RejectedReadDocument(SettingsReadFailureKind.TooLarge);
        }

        byte[] bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
        if (!AdmitsDocument())
        {
            return UnsafeReadDocument();
        }
        SettingsReadOutcome outcome = SettingsDocumentValidator.Validate(bytes);
        ExistingDocumentSnapshot document = outcome is SettingsRead
            ? new PresentDocument(
                WindowsLocalEntryIdentity.Describe(new FileInfo(_documentPath.CanonicalText)),
                bytes)
            : new BlockedDocument(SettingsWriteFailureKind.ExistingDocumentRejected);
        return new ReadDocumentObservation(outcome, document);
    }

    /// <summary>Captures the document a write would replace, or the failure that blocks writing.</summary>
    internal ExistingDocumentSnapshot Capture()
    {
        if (!AdmitsDocument())
        {
            return new BlockedDocument(SettingsWriteFailureKind.UnsafeLocation);
        }
        try
        {
            return CaptureAdmitted();
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

    /// <summary>
    /// Captures the just-published document and links it to the owned temporary entry; otherwise
    /// the next baseline is uncertain.
    /// </summary>
    internal ExistingDocumentSnapshot CapturePublished(byte[] serialized, string publishedIdentifier)
    {
        ExistingDocumentSnapshot captured = Capture();
        try
        {
            return captured is PresentDocument present &&
                present.Bytes.AsSpan().SequenceEqual(serialized) &&
                AdmitsDocument() &&
                WindowsFileIdentifier.Describe(_documentPath.CanonicalText).Equals(
                    publishedIdentifier,
                    StringComparison.Ordinal)
                ? present
                : UncertainDocument.Instance;
        }
        catch (IOException)
        {
            // The publish already completed. Keep the baseline uncertain when its identity cannot
            // be linked to the owned temporary entry instead of reporting the write as unpublished.
            return UncertainDocument.Instance;
        }
    }

    /// <summary>Reports whether the destination still equals the expected document.</summary>
    internal bool Matches(ExistingDocumentSnapshot expected)
    {
        if (!AdmitsDocument())
        {
            return false;
        }
        FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(_documentPath);
        if (expected is AbsentDocument)
        {
            return entry is null;
        }
        if (expected is not PresentDocument present || entry is null || !IsRegularFile(entry))
        {
            return false;
        }

        using FileStream stream = OpenRead(_documentPath.CanonicalText);
        if (stream.Length != present.Bytes.Length ||
            !AdmitsDocument() ||
            WindowsLocalEntryIdentity.Describe(new FileInfo(_documentPath.CanonicalText)) != present.Identity)
        {
            return false;
        }
        byte[] current = ReadExactBytes(stream);
        return AdmitsDocument() &&
            current.AsSpan().SequenceEqual(present.Bytes);
    }

    /// <summary>Reports whether the temporary entry is still the owned flushed file with exact bytes.</summary>
    internal bool TemporaryMatches(
        string temporaryText,
        string expectedIdentifier,
        byte[] expectedBytes)
    {
        if (!AdmitsDocument())
        {
            return false;
        }
        FileInfo temporary = new(temporaryText);
        if (!temporary.Exists ||
            (temporary.Attributes & FileAttributes.ReparsePoint) != 0 ||
            !AdmitsDocument() ||
            !WindowsFileIdentifier.Describe(temporaryText).Equals(
                expectedIdentifier,
                StringComparison.Ordinal))
        {
            return false;
        }
        using FileStream stream = OpenRead(temporaryText);
        return stream.Length == expectedBytes.Length &&
            ReadExactBytes(stream).AsSpan().SequenceEqual(expectedBytes) &&
            AdmitsDocument();
    }

    private ExistingDocumentSnapshot CaptureAdmitted()
    {
        FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(_documentPath);
        if (entry is null)
        {
            return AbsentDocument.Instance;
        }
        if (!IsRegularFile(entry))
        {
            return new BlockedDocument(SettingsWriteFailureKind.ExistingDocumentRejected);
        }

        using FileStream stream = OpenRead(_documentPath.CanonicalText);
        if (stream.Length > SettingsDocumentValidator.MaximumDocumentLength)
        {
            return new BlockedDocument(SettingsWriteFailureKind.ExistingDocumentRejected);
        }

        byte[] bytes = ReadExactBytes(stream);
        if (SettingsDocumentValidator.Validate(bytes) is not SettingsRead)
        {
            return new BlockedDocument(SettingsWriteFailureKind.ExistingDocumentRejected);
        }

        if (!AdmitsDocument())
        {
            return new BlockedDocument(SettingsWriteFailureKind.UnsafeLocation);
        }

        FileIdentity identity = WindowsLocalEntryIdentity.Describe(new FileInfo(_documentPath.CanonicalText));
        return new PresentDocument(identity, bytes);
    }

    private bool AdmitsDocument()
    {
        return _driveBoundary.AdmitsLocal(_documentPath);
    }

    private static ReadDocumentObservation UnsafeReadDocument()
    {
        return new ReadDocumentObservation(
            SettingsReadOutcome.Rejected(SettingsReadFailureKind.Unreadable),
            new BlockedDocument(SettingsWriteFailureKind.UnsafeLocation));
    }

    private static ReadDocumentObservation RejectedReadDocument(SettingsReadFailureKind failure)
    {
        return new ReadDocumentObservation(
            SettingsReadOutcome.Rejected(failure),
            new BlockedDocument(SettingsWriteFailureKind.ExistingDocumentRejected));
    }

    private static FileStream OpenRead(string path)
    {
        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
    }

    private static byte[] ReadExactBytes(FileStream stream)
    {
        byte[] bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static bool IsRegularFile(FileSystemInfo entry)
    {
        return entry is FileInfo file &&
            (file.Attributes & FileAttributes.ReparsePoint) == 0;
    }
}
