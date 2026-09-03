using System;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents the complete immutable state of one pane: what it shows and what it is doing.
/// </summary>
public sealed record PaneSnapshot
{
    private PaneSnapshot(PaneContent content, PaneActivity activity)
    {
        Content = content;
        Activity = activity;
    }

    /// <summary>Gets the listed content, or absence before the first successful read.</summary>
    public PaneContent Content { get; }

    /// <summary>Gets the read activity.</summary>
    public PaneActivity Activity { get; }

    /// <summary>Gets the snapshot of a pane that has never read a location.</summary>
    public static PaneSnapshot Initial { get; } = new(PaneContent.Absent, PaneActivity.Idle);

    internal PaneSnapshot WithActivity(PaneActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new PaneSnapshot(Content, activity);
    }

    internal static PaneSnapshot IdleWith(PaneContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new PaneSnapshot(content, PaneActivity.Idle);
    }
}
