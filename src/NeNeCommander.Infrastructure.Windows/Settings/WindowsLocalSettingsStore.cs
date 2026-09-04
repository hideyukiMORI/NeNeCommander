using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// Reads the persisted settings document from one Windows local file. The store is a pure
/// query: an absent document is reported, never created, and a rejected document is left
/// byte-for-byte as the person who wrote it left it.
/// </summary>
public sealed class WindowsLocalSettingsStore : ISettingsStore
{
    private readonly WindowsLocalPath _documentPath;

    /// <summary>Initializes the store with the composed location of the settings document.</summary>
    /// <param name="documentPath">Validated Windows local path of the settings document.</param>
    public WindowsLocalSettingsStore(WindowsLocalPath documentPath)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        _documentPath = documentPath;
    }

    /// <inheritdoc />
    public async Task<SettingsReadOutcome> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await ReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return SettingsReadOutcome.Absent();
        }
        catch (DirectoryNotFoundException)
        {
            return SettingsReadOutcome.Absent();
        }
        catch (UnauthorizedAccessException)
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Unreadable);
        }
        catch (IOException)
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Unreadable);
        }
    }

    /// <summary>
    /// Opens the document for shared reading, rejects it on length before any byte is decoded,
    /// and hands the bounded text to the sole validator.
    /// </summary>
    private async Task<SettingsReadOutcome> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        using FileStream stream = new(
            _documentPath.CanonicalText,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        if (stream.Length > SettingsDocumentValidator.MaximumDocumentLength)
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.TooLarge);
        }

        using StreamReader reader = new(stream, Encoding.UTF8);
        string text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return SettingsDocumentValidator.Validate(text);
    }
}
