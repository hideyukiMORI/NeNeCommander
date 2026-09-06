using System;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Input;

/// <summary>Represents one approved scheme selected in the session-owned settings editor.</summary>
public sealed record ColorSchemeSelection : UserIntent
{
    internal ColorSchemeSelection(ColorScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);
        Scheme = scheme;
    }

    /// <summary>Gets the approved selected scheme.</summary>
    public ColorScheme Scheme { get; }
}
