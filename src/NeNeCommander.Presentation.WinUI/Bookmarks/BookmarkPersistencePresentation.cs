using System;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>Bookmark-manager projection of the shared settings persistence channel.</summary>
public sealed record BookmarkPersistencePresentation
{
    internal BookmarkPersistencePresentation(
        SettingsSaveStatus saveStatus,
        SettingsWarningPresentation warning)
    {
        ArgumentNullException.ThrowIfNull(saveStatus);
        ArgumentNullException.ThrowIfNull(warning);
        SaveStatus = saveStatus;
        Warning = warning;
    }

    /// <summary>Gets the shared settings save status.</summary>
    public SettingsSaveStatus SaveStatus { get; }
    /// <summary>Gets the independent persistent settings warning.</summary>
    public SettingsWarningPresentation Warning { get; }
}
