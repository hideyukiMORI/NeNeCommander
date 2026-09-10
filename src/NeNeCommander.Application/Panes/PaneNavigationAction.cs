namespace NeNeCommander.Application.Panes;

/// <summary>
/// Identifies how one successful pane read changes the bounded location history. The action is
/// chosen before the read, but <see cref="PaneReducer"/> applies it only after success.
/// </summary>
internal abstract record PaneNavigationAction
{
    /// <summary>Gets the action for a normal location change.</summary>
    internal static PaneNavigationAction Append { get; } = new AppendAction();

    /// <summary>Gets the action for a refresh whose successful read keeps history unchanged.</summary>
    internal static PaneNavigationAction Preserve { get; } = new PreserveAction();

    /// <summary>Gets the action that commits one successful Back read.</summary>
    internal static PaneNavigationAction Back { get; } = new BackAction();

    /// <summary>Gets the action that commits one successful Forward read.</summary>
    internal static PaneNavigationAction Forward { get; } = new ForwardAction();

    private PaneNavigationAction()
    {
    }

    private sealed record AppendAction : PaneNavigationAction;
    private sealed record PreserveAction : PaneNavigationAction;
    private sealed record BackAction : PaneNavigationAction;
    private sealed record ForwardAction : PaneNavigationAction;
}
