using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Projects one inline editor from its pane's sole query and exact transient owner.</summary>
public sealed record PaneFilterPresentation
{
    internal PaneFilterPresentation(CommanderSnapshot snapshot, PaneSide side, PaneFilterPresentation? previous)
    {
        Side = side;
        SourcePane = snapshot.Panes.Of(side);
        SourceEditor = snapshot.Scopes.NameFilter;
        IsEnabled = SourcePane.Content is PaneContentListed && PaneInputOwnership.IsAvailable(snapshot);
        FocusEditor = IsEditing && previous?.IsEditing != true;
        FileListFocusSide = !ReferenceEquals(previous?.SourceEditor, SourceEditor) &&
            SourceEditor is PaneFilterClosed closed && closed.FileListFocusSide == side
                ? side
                : null;
    }

    /// <summary>Gets the permanent addressed pane, never inferred from native focus.</summary>
    public PaneSide Side { get; }

    /// <summary>Gets the exact snapshot used for qualified header and clear requests.</summary>
    public PaneSnapshot SourcePane { get; }

    /// <summary>Gets the shared editor state used to qualify callbacks.</summary>
    public PaneFilterState SourceEditor { get; }

    /// <summary>Gets this pane's exact current editor, or absence when another pane or scope owns input.</summary>
    public PaneFilterEditing? Editor => SourceEditor is PaneFilterEditing editing &&
        editing.Side == Side && ReferenceEquals(editing.SourcePane, SourcePane) ? editing : null;

    /// <summary>Gets whether this pane is currently being edited.</summary>
    public bool IsEditing => Editor is not null;

    /// <summary>Gets the pane-owned exact committed text without a second query state.</summary>
    public string Text => SourcePane.Content is PaneContentListed listed ? listed.State.NameFilter.Text : string.Empty;

    /// <summary>Gets whether any nonempty term restricts the displayed names.</summary>
    public bool IsRestricted => SourcePane.Content is PaneContentListed listed && !listed.State.NameFilter.IsEmpty;

    /// <summary>Gets whether editing or an applied restriction keeps the inline bar visible.</summary>
    public bool IsVisible => IsEditing || IsRestricted;

    /// <summary>Gets whether native entry may request editing; Application remains authoritative.</summary>
    public bool IsEnabled { get; }

    /// <summary>Gets whether this render newly enters editing, without refocusing on each character.</summary>
    public bool FocusEditor { get; }

    /// <summary>Gets whether this render may replace native text; ongoing edits retain pending text until notification.</summary>
    public bool SynchronizeText => !IsEditing || FocusEditor;

    /// <summary>Gets the one-time list focus effect, or absence for blur and repeated renders.</summary>
    public PaneSide? FileListFocusSide { get; }

    /// <summary>Gets a complete localized hint; zero results never claim that the folder is empty.</summary>
    public string HintResourceKey => IsRestricted && SourcePane.Content is PaneContentListed listed &&
        listed.State.VisibleEntries.Count == 0 ? "PaneFilterNoMatches"
            : IsEditing ? "PaneFilterEditingHint" : "PaneFilterAppliedHint";
}
