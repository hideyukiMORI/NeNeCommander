using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Input;

/// <summary>
/// Represents the closed set of user intents emitted by presentation input mappers.
/// </summary>
public abstract record UserIntent
{
    /// <summary>Gets the intent to focus the next visible item.</summary>
    public static UserIntent MoveNext { get; } = new MoveNextIntent();

    /// <summary>Gets the intent to focus the previous visible item.</summary>
    public static UserIntent MovePrevious { get; } = new MovePreviousIntent();

    /// <summary>Gets the intent to focus the first visible item.</summary>
    public static UserIntent FocusFirst { get; } = new FocusFirstIntent();

    /// <summary>Gets the intent to focus the last visible item.</summary>
    public static UserIntent FocusLast { get; } = new FocusLastIntent();

    /// <summary>Gets the intent to move down by half the visible page.</summary>
    public static UserIntent MoveHalfPageDown { get; } = new MoveHalfPageDownIntent();

    /// <summary>Gets the intent to move up by half the visible page.</summary>
    public static UserIntent MoveHalfPageUp { get; } = new MoveHalfPageUpIntent();

    /// <summary>Gets the intent to navigate to the parent location.</summary>
    public static UserIntent NavigateParent { get; } = new NavigateParentIntent();

    /// <summary>Gets the intent to open the focused item.</summary>
    public static UserIntent OpenFocused { get; } = new OpenFocusedIntent();

    /// <summary>Gets the intent to activate the other pane.</summary>
    public static UserIntent ActivateOtherPane { get; } = new ActivateOtherPaneIntent();

    /// <summary>Gets the intent to toggle explicit selection for the focus item.</summary>
    public static UserIntent ToggleSelection { get; } = new ToggleSelectionIntent();

    /// <summary>Gets the intent to toggle hidden and system entries in the active pane.</summary>
    public static UserIntent ToggleHiddenItems { get; } = new ToggleHiddenItemsIntent();

    /// <summary>Gets the intent to cancel transient state or clear selection.</summary>
    public static UserIntent Escape { get; } = new EscapeIntent();

    /// <summary>Gets the intent to begin rename.</summary>
    public static UserIntent Rename { get; } = new RenameIntent();

    /// <summary>Gets the intent to copy to the passive pane.</summary>
    public static UserIntent Copy { get; } = new CopyIntent();

    /// <summary>Gets the intent to move to the passive pane.</summary>
    public static UserIntent Move { get; } = new MoveIntent();

    /// <summary>Gets the intent to create a directory.</summary>
    public static UserIntent CreateDirectory { get; } = new CreateDirectoryIntent();

    /// <summary>Gets the intent to request deletion.</summary>
    public static UserIntent Delete { get; } = new DeleteIntent();

    /// <summary>Gets the intent to focus the address input.</summary>
    public static UserIntent FocusAddress { get; } = new FocusAddressIntent();

    /// <summary>Gets the intent to refresh the active pane.</summary>
    public static UserIntent Refresh { get; } = new RefreshIntent();

    /// <summary>Gets the intent to confirm the pending modal question.</summary>
    public static UserIntent Confirm { get; } = new ConfirmIntent();

    /// <summary>Gets the intent to open the session-owned settings editor.</summary>
    public static UserIntent OpenSettings { get; } = new OpenSettingsIntent();

    private protected UserIntent()
    {
    }

    /// <summary>Creates the intent that confirms a name-entry modal with the text the user typed.</summary>
    /// <param name="name">Untrusted name text; validation belongs to the request built from it.</param>
    /// <returns>A <see cref="NameSubmission"/> carrying the text verbatim.</returns>
    public static UserIntent SubmitName(string name)
    {
        return new NameSubmission(name);
    }

    /// <summary>Creates an explicit conflict-resolution submission from the modal.</summary>
    public static UserIntent ResolveConflict(
        TransferConflictDecision decision,
        TransferConflictScope scope)
    {
        return new ConflictDecisionSubmission(decision, scope);
    }

    /// <summary>Creates an intent selecting one approved color scheme.</summary>
    /// <param name="scheme">Approved scheme selected by the editor.</param>
    /// <returns>The typed selection intent.</returns>
    public static UserIntent SelectColorScheme(ColorScheme scheme)
    {
        return new ColorSchemeSelection(scheme);
    }

    /// <summary>Creates an intent selecting the next-launch hidden-item default.</summary>
    /// <param name="visibility">Closed visibility selected by the editor.</param>
    /// <returns>The typed selection intent.</returns>
    public static UserIntent SelectLaunchHiddenItemVisibility(HiddenItemVisibility visibility)
    {
        return new LaunchHiddenItemVisibilitySelection(visibility);
    }

    private sealed record MoveNextIntent : UserIntent;
    private sealed record MovePreviousIntent : UserIntent;
    private sealed record FocusFirstIntent : UserIntent;
    private sealed record FocusLastIntent : UserIntent;
    private sealed record MoveHalfPageDownIntent : UserIntent;
    private sealed record MoveHalfPageUpIntent : UserIntent;
    private sealed record NavigateParentIntent : UserIntent;
    private sealed record OpenFocusedIntent : UserIntent;
    private sealed record ActivateOtherPaneIntent : UserIntent;
    private sealed record ToggleSelectionIntent : UserIntent;
    private sealed record ToggleHiddenItemsIntent : UserIntent;
    private sealed record EscapeIntent : UserIntent;
    private sealed record RenameIntent : UserIntent;
    private sealed record CopyIntent : UserIntent;
    private sealed record MoveIntent : UserIntent;
    private sealed record CreateDirectoryIntent : UserIntent;
    private sealed record DeleteIntent : UserIntent;
    private sealed record FocusAddressIntent : UserIntent;
    private sealed record RefreshIntent : UserIntent;
    private sealed record ConfirmIntent : UserIntent;
    private sealed record OpenSettingsIntent : UserIntent;
}
