namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>Closed projection of whether one manager option matches the session selection.</summary>
public abstract record BookmarkOptionSelection
{
    /// <summary>Gets the selected option state.</summary>
    public static BookmarkOptionSelection Selected { get; } = new SelectedState();
    /// <summary>Gets the unselected option state.</summary>
    public static BookmarkOptionSelection NotSelected { get; } = new NotSelectedState();

    private BookmarkOptionSelection()
    {
    }

    private sealed record SelectedState : BookmarkOptionSelection;
    private sealed record NotSelectedState : BookmarkOptionSelection;
}
