namespace NeNeCommander.Application.Commands;

/// <summary>Names the closed state-level reason a catalog command cannot start.</summary>
public abstract record CommandUnavailableReason
{
    /// <summary>Gets the reason that the active pane has no focused entry.</summary>
    public static CommandUnavailableReason FocusRequired { get; } = new FocusRequiredReason();

    /// <summary>Gets the reason that the active pane has neither selected entries nor a focused entry.</summary>
    public static CommandUnavailableReason SourceRequired { get; } = new SourceRequiredReason();

    /// <summary>Gets the reason that the passive pane has no listed destination.</summary>
    public static CommandUnavailableReason PassivePaneUnavailable { get; } = new PassivePaneUnavailableReason();

    /// <summary>Gets the reason that the active location has no parent.</summary>
    public static CommandUnavailableReason ParentUnavailable { get; } = new ParentUnavailableReason();

    /// <summary>Gets the reason that the active pane has no earlier successful location.</summary>
    public static CommandUnavailableReason BackUnavailable { get; } = new BackUnavailableReason();

    /// <summary>Gets the reason that the active pane has no later successful location.</summary>
    public static CommandUnavailableReason ForwardUnavailable { get; } = new ForwardUnavailableReason();

    private CommandUnavailableReason()
    {
    }

    private sealed record FocusRequiredReason : CommandUnavailableReason;
    private sealed record SourceRequiredReason : CommandUnavailableReason;
    private sealed record PassivePaneUnavailableReason : CommandUnavailableReason;
    private sealed record ParentUnavailableReason : CommandUnavailableReason;
    private sealed record BackUnavailableReason : CommandUnavailableReason;
    private sealed record ForwardUnavailableReason : CommandUnavailableReason;
}
