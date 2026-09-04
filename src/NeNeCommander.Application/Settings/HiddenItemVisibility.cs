namespace NeNeCommander.Application.Settings;

/// <summary>
/// Identifies the closed visibility of hidden and system entries in a pane listing. The concept
/// is a closed state rather than a flag so no impossible combination can be constructed.
/// </summary>
public abstract record HiddenItemVisibility
{
    /// <summary>Gets the visibility that lists hidden and system entries.</summary>
    public static HiddenItemVisibility Shown { get; } = new ShownVisibility();

    /// <summary>Gets the visibility that omits hidden and system entries.</summary>
    public static HiddenItemVisibility Hidden { get; } = new HiddenVisibility();

    private HiddenItemVisibility()
    {
    }

    private sealed record ShownVisibility : HiddenItemVisibility;
    private sealed record HiddenVisibility : HiddenItemVisibility;
}
