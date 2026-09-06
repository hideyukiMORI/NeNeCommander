using System;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.App.Views;

/// <summary>Provides one approved scheme selected for composition-root application.</summary>
public sealed class ColorSchemeChangedEventArgs : EventArgs
{
    /// <summary>Initializes the event with one approved scheme.</summary>
    /// <param name="scheme">Approved selected scheme.</param>
    public ColorSchemeChangedEventArgs(ColorScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);
        Scheme = scheme;
    }

    /// <summary>Gets the approved selected scheme.</summary>
    public ColorScheme Scheme { get; }
}
