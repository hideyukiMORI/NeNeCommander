using System;
using NeNeCommander.Application.Time;

namespace NeNeCommander.Presentation.WinUI.Tests;

internal sealed class AdjustableClock : IClock
{
    private TimeSpan _time;

    private AdjustableClock()
    {
        _time = TimeSpan.Zero;
    }

    internal static AdjustableClock Create()
    {
        return new AdjustableClock();
    }

    internal void Advance(TimeSpan duration)
    {
        _time += duration;
    }

    public TimeSpan GetMonotonicTime()
    {
        return _time;
    }
}
