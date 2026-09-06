using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Settings;

/// <summary>
/// Owns the current settings selection, modal editor state, and one ordered write queue. A choice
/// becomes session state before I/O; an older completion never replaces a newer choice.
/// </summary>
public sealed class SettingsSession
{
    private readonly Lock _sync = new();
    private readonly Action<Exception> _defectObserver;
    private readonly BookmarkEditorSession _bookmarkEditor = new();
    private readonly ISettingsStore _store;
    private SettingsEditorState _editor;
    private SettingsPersistenceState _persistence;
    private UserSettings _settings;
    private Task _writeTail;
    private long _revision;
    private bool _stopped;

    /// <summary>Initializes the owner from the one startup read outcome.</summary>
    /// <param name="store">Sole settings boundary.</param>
    /// <param name="initialOutcome">Startup read outcome retained for diagnostics.</param>
    /// <param name="defectObserver">Host callback that observes unexpected queued-write faults.</param>
    public SettingsSession(
        ISettingsStore store,
        SettingsReadOutcome initialOutcome,
        Action<Exception> defectObserver)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(initialOutcome);
        ArgumentNullException.ThrowIfNull(defectObserver);
        _store = store;
        _defectObserver = defectObserver;
        _settings = initialOutcome is SettingsRead read ? read.Settings : UserSettings.Default;
        _editor = SettingsEditorState.Closed;
        _persistence = initialOutcome is SettingsRejected rejected
            ? SettingsPersistenceState.StartupRejected(rejected.Kind)
            : SettingsPersistenceState.Succeeded;
        _writeTail = Task.CompletedTask;
    }

    /// <summary>Gets the current immutable settings interaction state.</summary>
    public SettingsSnapshot Current
    {
        get
        {
            lock (_sync)
            {
                return Snapshot();
            }
        }
    }

    /// <summary>Opens the settings modal without changing either preference.</summary>
    /// <returns>The current open snapshot.</returns>
    public SettingsSnapshot Open()
    {
        lock (_sync)
        {
            _editor = SettingsEditorState.Open;
            return Snapshot();
        }
    }

    /// <summary>Opens the bookmark catalog editor without changing metadata.</summary>
    public SettingsSnapshot OpenBookmarks()
    {
        lock (_sync)
        {
            _bookmarkEditor.Open();
            _editor = SettingsEditorState.Bookmarks;
            return Snapshot();
        }
    }

    /// <summary>Closes the settings modal without rolling back save-on-change selections.</summary>
    /// <returns>The current closed snapshot.</returns>
    public SettingsSnapshot Close()
    {
        lock (_sync)
        {
            _bookmarkEditor.Close();
            _editor = SettingsEditorState.Closed;
            return Snapshot();
        }
    }

    /// <summary>Selects and queues one approved color scheme.</summary>
    /// <param name="scheme">Approved scheme.</param>
    /// <param name="observer">Receives the state current after this write completes.</param>
    /// <param name="cancellationToken">Token observed before this write mutates storage.</param>
    /// <returns>A task completing after this revision's ordered write attempt.</returns>
    /// <exception cref="InvalidOperationException">The session has already stopped.</exception>
    public Task SelectColorSchemeAsync(
        ColorScheme scheme,
        ISettingsProgressObserver observer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(observer);
        lock (_sync)
        {
            return QueueWrite(
                UserSettings.Create(scheme, _settings.HiddenItemVisibility, _settings.Bookmarks),
                observer,
                cancellationToken);
        }
    }

    /// <summary>Selects and queues the next-launch hidden-item default.</summary>
    /// <param name="visibility">Closed launch default.</param>
    /// <param name="observer">Receives the state current after this write completes.</param>
    /// <param name="cancellationToken">Token observed before this write mutates storage.</param>
    /// <returns>A task completing after this revision's ordered write attempt.</returns>
    /// <exception cref="InvalidOperationException">The session has already stopped.</exception>
    public Task SelectLaunchHiddenItemVisibilityAsync(
        HiddenItemVisibility visibility,
        ISettingsProgressObserver observer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visibility);
        ArgumentNullException.ThrowIfNull(observer);
        lock (_sync)
        {
            return QueueWrite(
                UserSettings.Create(_settings.ColorScheme, visibility, _settings.Bookmarks),
                observer,
                cancellationToken);
        }
    }

    /// <summary>Accepts and queues one complete bookmark catalog while preserving preferences.</summary>
    /// <param name="catalog">Complete validated replacement catalog.</param>
    /// <param name="observer">Receives the state current after this write completes.</param>
    /// <param name="cancellationToken">Token observed before this write mutates storage.</param>
    /// <returns>A task completing after this revision's ordered write attempt.</returns>
    /// <exception cref="InvalidOperationException">The session has already stopped.</exception>
    internal Task SaveBookmarkCatalogAsync(
        BookmarkCatalog catalog,
        ISettingsProgressObserver observer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(observer);
        lock (_sync)
        {
            return QueueWrite(
                UserSettings.Create(
                    _settings.ColorScheme,
                    _settings.HiddenItemVisibility,
                    catalog),
                observer,
                cancellationToken);
        }
    }

    internal Task ApplyBookmarkEditorAction(
        BookmarkEditorAction action,
        BookmarkRegistrationDefaults defaults,
        ISettingsProgressObserver observer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(defaults);
        ArgumentNullException.ThrowIfNull(observer);
        lock (_sync)
        {
            if (_stopped)
            {
                throw new InvalidOperationException(
                    "Settings persistence cannot restart after shutdown.");
            }
            BookmarkEditorTransition transition = _bookmarkEditor.Apply(
                action,
                _settings.Bookmarks,
                defaults);
            if (transition is BookmarkEditorTransition.CloseRequested)
            {
                _bookmarkEditor.Close();
                _editor = SettingsEditorState.Closed;
                return Task.CompletedTask;
            }
            return transition is BookmarkEditorTransition.CatalogChanged changed
                ? QueueWrite(
                    UserSettings.Create(
                        _settings.ColorScheme,
                        _settings.HiddenItemVisibility,
                        changed.Catalog),
                    observer,
                    cancellationToken)
                : Task.CompletedTask;
        }
    }

    internal BookmarkNavigationStart BeginBookmarkNavigation(BookmarkSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        lock (_sync)
        {
            BookmarkSelection? current = _settings.Bookmarks.Select(selection.Key);
            return current is not null && _settings.Bookmarks.Matches(selection) &&
                _bookmarkEditor.BeginNavigation(current, _settings.Bookmarks)
                ? new BookmarkNavigationStart.Accepted(current.Entry)
                : new BookmarkNavigationStart.Rejected();
        }
    }

    internal void FinishBookmarkNavigationSucceeded()
    {
        lock (_sync)
        {
            _bookmarkEditor.FinishNavigationSucceeded();
            _editor = SettingsEditorState.Closed;
        }
    }

    internal void FinishBookmarkNavigationFailed(PaneActivity reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        lock (_sync)
        {
            _bookmarkEditor.FinishNavigationFailed(reason);
        }
    }

    /// <summary>Awaits the ordered queue through linearized shutdown and then closes it.</summary>
    public async Task StopAsync()
    {
        while (true)
        {
            Task tail;
            lock (_sync)
            {
                if (_stopped)
                {
                    return;
                }
                tail = _writeTail;
            }
            await AwaitCompletionAsync(tail).ConfigureAwait(false);
            lock (_sync)
            {
                if (ReferenceEquals(tail, _writeTail))
                {
                    _stopped = true;
                    return;
                }
            }
        }
    }

    private Task QueueWrite(
        UserSettings settings,
        ISettingsProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (_stopped)
        {
            throw new InvalidOperationException("Settings persistence cannot restart after shutdown.");
        }
        _settings = settings;
        _persistence = SettingsPersistenceState.Pending;
        long revision = ++_revision;
        Task predecessor = _writeTail;
        Task write = PersistAfterAsync(predecessor, settings, revision, observer, cancellationToken);
        _writeTail = ObserveCompletion(write);
        return write;
    }

    private Task ObserveCompletion(Task write)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        write.GetAwaiter().OnCompleted(() =>
        {
            try
            {
                if (write.Exception is AggregateException aggregate)
                {
                    _defectObserver(aggregate.GetBaseException());
                }
            }
            finally
            {
                completion.SetResult();
            }
        });
        return completion.Task;
    }

    private async Task PersistAfterAsync(
        Task predecessor,
        UserSettings settings,
        long revision,
        ISettingsProgressObserver observer,
        CancellationToken cancellationToken)
    {
        // Completion, rather than success, orders revisions. The queue-owned predecessor has
        // synchronously reported any write defect before its completion source releases this revision.
        await AwaitCompletionAsync(predecessor).ConfigureAwait(false);

        SettingsWriteOutcome outcome;
        try
        {
            outcome = await _store.WriteAsync(settings, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            CompleteRevision(revision, SettingsPersistenceState.Cancelled);
            observer.SettingsProgressed(Current);
            throw;
        }

        SettingsPersistenceState persistence = outcome is SettingsWriteRejected rejected
            ? SettingsPersistenceState.Failed(rejected)
            : SettingsPersistenceState.Succeeded;
        CompleteRevision(revision, persistence);
        observer.SettingsProgressed(Current);
    }

    private void CompleteRevision(long revision, SettingsPersistenceState persistence)
    {
        lock (_sync)
        {
            if (revision == _revision)
            {
                _persistence = persistence;
            }
        }
    }

    private static async Task AwaitCompletionAsync(Task task)
    {
        _ = await Task.WhenAny(task).ConfigureAwait(false);
        _ = task.Exception;
    }

    private SettingsSnapshot Snapshot()
    {
        return new SettingsSnapshot(_settings, _editor, _bookmarkEditor.Current, _persistence);
    }

}
