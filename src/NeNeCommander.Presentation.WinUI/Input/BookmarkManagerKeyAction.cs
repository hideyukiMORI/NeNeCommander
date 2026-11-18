namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Names the closed Presentation-owned action of one bookmark-manager key (ADR-0080). An action is
/// region-neutral: what it does depends on whether the category column or the bookmark list holds
/// focus, which <c>BookmarkManagerKeyboard</c> decides.
/// </summary>
public abstract record BookmarkManagerKeyAction
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the action that moves to the next item of the focused region.</summary>
    public static BookmarkManagerKeyAction MoveNext { get; } = new MoveNextAction();

    /// <summary>Gets the action that moves to the previous item of the focused region.</summary>
    public static BookmarkManagerKeyAction MovePrevious { get; } = new MovePreviousAction();

    /// <summary>Gets the action that opens the focused bookmark, or moves from the categories to the list.</summary>
    public static BookmarkManagerKeyAction Activate { get; } = new ActivateAction();

    /// <summary>Gets the action that moves focus to the search field.</summary>
    public static BookmarkManagerKeyAction FocusSearch { get; } = new FocusSearchAction();

    /// <summary>Gets the action that moves focus between the category column and the bookmark list.</summary>
    public static BookmarkManagerKeyAction SwitchRegion { get; } = new SwitchRegionAction();

    /// <summary>Gets the action that registers the current folder or adds a category.</summary>
    public static BookmarkManagerKeyAction Add { get; } = new AddAction();

    /// <summary>Gets the action that edits the selected bookmark or renames the selected category.</summary>
    public static BookmarkManagerKeyAction Edit { get; } = new EditAction();

    /// <summary>Gets the action that deletes the selected bookmark or the selected category.</summary>
    public static BookmarkManagerKeyAction Delete { get; } = new DeleteAction();

    /// <summary>Gets the action that closes the manager through the existing <c>Escape</c> route.</summary>
    public static BookmarkManagerKeyAction Close { get; } = new CloseAction();

    /// <summary>Gets the action that leaves the search field for the bookmark list.</summary>
    public static BookmarkManagerKeyAction LeaveSearch { get; } = new LeaveSearchAction();

    private BookmarkManagerKeyAction()
    {
    }

    /// <summary>Gets whether a held key repeats the action; every other action runs once per press.</summary>
    public virtual bool Repeats => false;

    private sealed record MoveNextAction : BookmarkManagerKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override bool Repeats => true;
    }
    private sealed record MovePreviousAction : BookmarkManagerKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }

        public override bool Repeats => true;
    }
    private sealed record ActivateAction : BookmarkManagerKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record FocusSearchAction : BookmarkManagerKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record SwitchRegionAction : BookmarkManagerKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record AddAction : BookmarkManagerKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record EditAction : BookmarkManagerKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record DeleteAction : BookmarkManagerKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record CloseAction : BookmarkManagerKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record LeaveSearchAction : BookmarkManagerKeyAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
