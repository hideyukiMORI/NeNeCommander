using System;
using System.Diagnostics;
using NeNeCommander.Application.Time;

namespace NeNeCommander.Infrastructure.Windows.Time;

/// <summary>Supplies process-independent monotonic elapsed time from the platform stopwatch.</summary>
public sealed class StopwatchClock : IClock
{
    /// <inheritdoc />
    public TimeSpan GetMonotonicTime()
    {
        return Stopwatch.GetElapsedTime(0);
    }
}
