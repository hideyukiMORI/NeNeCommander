namespace NeNeCommander.Application.Input;

/// <summary>Defines the closed actions admitted for one qualified pane gesture.</summary>
public abstract record PaneEntryAction
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the Select action.</summary>
    public static PaneEntryAction Select { get; } = new SelectAction();

    /// <summary>Gets the ToggleSelection action.</summary>
    public static PaneEntryAction ToggleSelection { get; } = new ToggleSelectionAction();

    /// <summary>Gets the SelectRange action.</summary>
    public static PaneEntryAction SelectRange { get; } = new SelectRangeAction();

    /// <summary>Gets the AddRange action.</summary>
    public static PaneEntryAction AddRange { get; } = new AddRangeAction();

    /// <summary>Gets the Open action.</summary>
    public static PaneEntryAction Open { get; } = new OpenAction();

    /// <summary>Gets focus with selected-batch preservation before an item-context capture.</summary>
    public static PaneEntryAction OpenContextMenu { get; } = new OpenContextMenuAction();

    private PaneEntryAction()
    {
    }

    private sealed record SelectAction : PaneEntryAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record ToggleSelectionAction : PaneEntryAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record SelectRangeAction : PaneEntryAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record AddRangeAction : PaneEntryAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record OpenAction : PaneEntryAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record OpenContextMenuAction : PaneEntryAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
