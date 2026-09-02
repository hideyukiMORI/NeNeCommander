using System;

namespace NeNeCommander.Application.Time;

/// <summary>
/// Supplies monotonic elapsed time for deterministic interaction behavior.
/// </summary>
public interface IClock
{
    /// <summary>Gets the current monotonic timestamp.</summary>
    public TimeSpan GetMonotonicTime();
}
