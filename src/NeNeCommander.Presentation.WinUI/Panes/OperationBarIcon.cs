namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies the closed icon the operation bar shows before its status text. The framework cannot
/// bind one element to a vector geometry chosen at run time, so the bar holds one element per shape
/// and shows the one this value names.
/// </summary>
public abstract record OperationBarIcon
{
    /// <summary>Gets the icon of a bar that shows no shape.</summary>
    public static OperationBarIcon None { get; } = new NoIcon();

    /// <summary>Gets the warning triangle shown while a destructive question or a failure stands.</summary>
    public static OperationBarIcon Warning { get; } = new WarningIcon();

    /// <summary>Gets the pen shown while the bar waits for a typed name.</summary>
    public static OperationBarIcon NameEntry { get; } = new NameEntryIcon();

    private OperationBarIcon()
    {
    }

    private sealed record NoIcon : OperationBarIcon;
    private sealed record WarningIcon : OperationBarIcon;
    private sealed record NameEntryIcon : OperationBarIcon;
}
