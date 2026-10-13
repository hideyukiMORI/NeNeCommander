using System;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents the order one pane projects its listing in: one closed <see cref="SortKey"/> and one
/// closed <see cref="SortDirection"/>. It is pane state owned by <see cref="PaneReducer"/>, carried
/// unchanged through reads, refresh, and history navigation of the same pane. Settings retain
/// accepted choices for the next launch through the same typed construction path.
/// </summary>
public sealed record PaneSortOrder
{
    private PaneSortOrder(SortKey key, SortDirection direction)
    {
        Key = key;
        Direction = direction;
    }

    /// <summary>Gets the order every pane starts with: name ascending.</summary>
    public static PaneSortOrder Default { get; } = Create(SortKey.Name, SortDirection.Ascending);

    /// <summary>Creates an order from one validated closed key and direction.</summary>
    public static PaneSortOrder Create(SortKey key, SortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(direction);
        return new PaneSortOrder(key, direction);
    }

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
            ? Create(Key, Direction.Reversed)
            : Create(requested, SortDirection.Ascending);
    }
}
