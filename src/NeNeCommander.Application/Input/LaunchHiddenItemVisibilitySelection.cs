using System;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Input;

/// <summary>Represents the next-launch hidden-item default selected in settings.</summary>
public sealed record LaunchHiddenItemVisibilitySelection : UserIntent
{
    internal LaunchHiddenItemVisibilitySelection(HiddenItemVisibility visibility)
    {
        ArgumentNullException.ThrowIfNull(visibility);
        Visibility = visibility;
    }

    /// <summary>Gets the closed selected launch default.</summary>
    public HiddenItemVisibility Visibility { get; }
}
