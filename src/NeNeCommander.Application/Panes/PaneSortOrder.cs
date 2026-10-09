using System;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents the order one pane projects its listing in: one closed <see cref="SortKey"/> and one
/// closed <see cref="SortDirection"/>. It is pane state owned by <see cref="PaneReducer"/>, carried
/// unchanged through reads, refresh, and history navigation of the same pane, and not persisted.
/// Every value is reached from <see cref="Default"/> through <see cref="Toggle"/>, so no other
/// construction path exists.
/// </summary>
public sealed record PaneSortOrder
{
    private PaneSortOrder(SortKey key, SortDirection direction)
    {
        Key = key;
        Direction = direction;
    }

    /// <summary>Gets the order every pane starts with: name ascending.</summary>
    public static PaneSortOrder Default { get; } = new(SortKey.Name, SortDirection.Ascending);

    /// <summary>Gets the key that orders entries inside each kind group.</summary>
    public SortKey Key { get; }

    /// <summary>Gets the direction of the key comparison.</summary>
    public SortDirection Direction { get; }

    /// <summary>
    /// Returns the order a sort request for one key produces: the same key again reverses the
    /// direction, and a different key starts ascending.
    /// </summary>
    /// <param name="requested">Key the user asked to sort by.</param>
    /// <returns>The next order; this value is never changed.</returns>
    public PaneSortOrder Toggle(SortKey requested)
    {
        ArgumentNullException.ThrowIfNull(requested);
        return requested == Key
            ? new PaneSortOrder(Key, Direction.Reversed)
            : new PaneSortOrder(requested, SortDirection.Ascending);
    }
}
