using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

internal sealed class ScriptedSettingsStore : ISettingsStore
{
    private readonly Lock _sync = new();
    private readonly Queue<TaskCompletionSource<SettingsWriteOutcome>> _plannedWrites = new();
    private readonly List<UserSettings> _writes = [];
    private readonly SettingsReadOutcome _readOutcome;

    internal ScriptedSettingsStore(SettingsReadOutcome readOutcome)
    {
        _readOutcome = readOutcome;
    }

    internal IReadOnlyList<UserSettings> Writes
    {
        get
        {
            lock (_sync)
            {
                return _writes.ToArray();
            }
        }
    }

    internal TaskCompletionSource<SettingsWriteOutcome> PlanWrite()
    {
        TaskCompletionSource<SettingsWriteOutcome> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            _plannedWrites.Enqueue(completion);
        }
        return completion;
    }

    public Task<SettingsReadOutcome> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_readOutcome);
    }

    public Task<SettingsWriteOutcome> WriteAsync(
        UserSettings settings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            _writes.Add(settings);
            return _plannedWrites.Dequeue().Task;
        }
    }
}
