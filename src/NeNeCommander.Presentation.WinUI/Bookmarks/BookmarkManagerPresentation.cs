using System;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>Complete render-ready state of the dedicated bookmark-manager overlay.</summary>
public sealed record BookmarkManagerPresentation
{
    internal BookmarkManagerPresentation(
        SettingsEditorState editor,
        BookmarkBrowsePresentation browse,
        BookmarkEditorDetails details,
        BookmarkPersistencePresentation persistence)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(browse);
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(persistence);
        IsOpen = editor == SettingsEditorState.Bookmarks;
        Browse = browse;
        Details = details;
        Persistence = persistence;
    }

    /// <summary>Gets whether the bookmark manager owns modal input.</summary>
    public bool IsOpen { get; }
    /// <summary>Gets the retained browse projection.</summary>
    public BookmarkBrowsePresentation Browse { get; }
    /// <summary>Gets the current nested editor projection.</summary>
    public BookmarkEditorDetails Details { get; }
    /// <summary>Gets the shared persistence projection.</summary>
    public BookmarkPersistencePresentation Persistence { get; }
}
