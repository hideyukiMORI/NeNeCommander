namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents a key the bookmark manager owns and the action it emits (ADR-0080).</summary>
public sealed record MappedBookmarkManagerAction : KeyboardMappingOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal MappedBookmarkManagerAction(BookmarkManagerKeyAction action)
    {
        Action = action;
    }

    /// <summary>Gets the bookmark-manager action.</summary>
    public BookmarkManagerKeyAction Action { get; }
}
