using System.Threading;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// Holds the document and location baselines that later writes must still observe. Reads and
/// published writes replace them; the first write without a read captures them once.
/// </summary>
internal sealed class SettingsBaseline
{
    private readonly Lock _sync = new();
    private readonly SettingsLocationObserver _locations;
    private readonly SettingsDocumentObserver _documents;
    private ExistingDocumentSnapshot? _expectedDocument;
    private SettingsLocationSnapshot? _expectedLocation;

    internal SettingsBaseline(SettingsLocationObserver locations, SettingsDocumentObserver documents)
    {
        _locations = locations;
        _documents = documents;
    }

    internal void RememberDocument(ExistingDocumentSnapshot document)
    {
        lock (_sync)
        {
            _expectedDocument = document;
        }
    }

    internal void RememberLocation(SettingsLocationSnapshot location)
    {
        lock (_sync)
        {
            _expectedLocation = location;
        }
    }

    internal void Remember(
        ExistingDocumentSnapshot document,
        SettingsLocationSnapshot location)
    {
        lock (_sync)
        {
            _expectedDocument = document;
            _expectedLocation = location;
        }
    }

    internal ExistingDocumentSnapshot DocumentForWrite()
    {
        lock (_sync)
        {
            if (_expectedDocument is not null)
            {
                return _expectedDocument;
            }
        }

        ExistingDocumentSnapshot captured = _documents.Capture();
        lock (_sync)
        {
            _expectedDocument ??= captured;
            return _expectedDocument;
        }
    }

    internal SettingsLocationSnapshot LocationForWrite()
    {
        lock (_sync)
        {
            if (_expectedLocation is not null)
            {
                return _expectedLocation;
            }
        }

        SettingsLocationSnapshot captured = _locations.Capture();
        lock (_sync)
        {
            _expectedLocation ??= captured;
            return _expectedLocation;
        }
    }

    internal void RememberPublishedLocation(SafeLocation approved)
    {
        RememberLocation(_locations.ConfirmPublished(approved));
    }

    internal void RememberPublishedDocument(byte[] serialized, string publishedIdentifier)
    {
        RememberDocument(_documents.CapturePublished(serialized, publishedIdentifier));
    }
}
