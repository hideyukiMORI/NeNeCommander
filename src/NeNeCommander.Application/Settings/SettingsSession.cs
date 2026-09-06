using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Application.Settings;

/// <summary>
/// Owns the current settings selection, modal editor state, and one ordered write queue. A choice
/// becomes session state before I/O; an older completion never replaces a newer choice.
/// </summary>
public sealed class SettingsSession
{
    private readonly Lock _sync = new();
    private readonly Action<Exception> _defectObserver;
    private readonly ISettingsStore _store;
    private SettingsEditorState _editor;
    private SettingsPersistenceState _persistence;
    private UserSettings _settings;
    private Task _writeTail;
    private long _revision;

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

    /// <summary>Closes the settings modal without rolling back save-on-change selections.</summary>
    /// <returns>The current closed snapshot.</returns>
    public SettingsSnapshot Close()
    {
        lock (_sync)
        {
            _editor = SettingsEditorState.Closed;
            return Snapshot();
        }
    }

    /// <summary>Selects and queues one approved color scheme.</summary>
    /// <param name="scheme">Approved scheme.</param>
    /// <param name="observer">Receives the state current after this write completes.</param>
    /// <param name="cancellationToken">Token observed before this write mutates storage.</param>
    /// <returns>A task completing after this revision's ordered write attempt.</returns>
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
                UserSettings.Create(scheme, _settings.HiddenItemVisibility),
                observer,
                cancellationToken);
        }
    }

    /// <summary>Selects and queues the next-launch hidden-item default.</summary>
    /// <param name="visibility">Closed launch default.</param>
    /// <param name="observer">Receives the state current after this write completes.</param>
    /// <param name="cancellationToken">Token observed before this write mutates storage.</param>
    /// <returns>A task completing after this revision's ordered write attempt.</returns>
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
                UserSettings.Create(_settings.ColorScheme, visibility),
                observer,
                cancellationToken);
        }
    }

    /// <summary>Awaits every settings write queued before shutdown began.</summary>
    public async Task StopAsync()
    {
        while (true)
        {
            Task tail;
            lock (_sync)
            {
                tail = _writeTail;
            }
            await AwaitCompletionAsync(tail).ConfigureAwait(false);
            lock (_sync)
            {
                if (ReferenceEquals(tail, _writeTail))
                {
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
        _settings = settings;
        _persistence = SettingsPersistenceState.Pending;
        long revision = ++_revision;
        Task predecessor = _writeTail;
        Task write = PersistAfterAsync(predecessor, settings, revision, observer, cancellationToken);
        _writeTail = ObserveCompletionAsync(write);
        return write;
    }

    private async Task ObserveCompletionAsync(Task write)
    {
        await AwaitCompletionAsync(write).ConfigureAwait(false);
        if (write.Exception is not AggregateException aggregate)
        {
            return;
        }

        _defectObserver(aggregate.GetBaseException());
    }

    private async Task PersistAfterAsync(
        Task predecessor,
        UserSettings settings,
        long revision,
        ISettingsProgressObserver observer,
        CancellationToken cancellationToken)
    {
        // Completion, rather than success, orders revisions. The queue-owned predecessor has
        // observed any write defect before it completes, and its own callback fault cannot strand
        // this revision.
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
        return new SettingsSnapshot(_settings, _editor, _persistence);
    }

}
