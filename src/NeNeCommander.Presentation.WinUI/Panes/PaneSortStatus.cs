using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies the closed sort indication a listed pane shows beside its status: one per key and
/// direction. Each indication names a localization resource; no user-facing text is assembled in
/// code.
/// </summary>
public sealed record PaneSortStatus
{
    /// <summary>Gets the indication of a name order, ascending.</summary>
    public static PaneSortStatus NameAscending { get; } = new("PaneSortNameAscending");

    /// <summary>Gets the indication of a name order, descending.</summary>
    public static PaneSortStatus NameDescending { get; } = new("PaneSortNameDescending");

    /// <summary>Gets the indication of an extension order, ascending.</summary>
    public static PaneSortStatus ExtensionAscending { get; } = new("PaneSortExtensionAscending");

    /// <summary>Gets the indication of an extension order, descending.</summary>
    public static PaneSortStatus ExtensionDescending { get; } = new("PaneSortExtensionDescending");

    /// <summary>Gets the indication of a size order, ascending.</summary>
    public static PaneSortStatus SizeAscending { get; } = new("PaneSortSizeAscending");

    /// <summary>Gets the indication of a size order, descending.</summary>
    public static PaneSortStatus SizeDescending { get; } = new("PaneSortSizeDescending");

    /// <summary>Gets the indication of a modification-time order, ascending.</summary>
    public static PaneSortStatus ModifiedAscending { get; } = new("PaneSortModifiedAscending");

    /// <summary>Gets the indication of a modification-time order, descending.</summary>
    public static PaneSortStatus ModifiedDescending { get; } = new("PaneSortModifiedDescending");

    private PaneSortStatus(string resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>Gets the localization resource key that names this indication.</summary>
    public string ResourceKey { get; }

    /// <summary>
    /// Translates one pane sort order into its closed indication. <see cref="SortKey"/> is a closed
    /// set of four keys, so after name, extension, and size the remaining key is the modification
    /// time.
    /// </summary>
    /// <param name="order">Sort order the pane state holds.</param>
    /// <returns>The indication of that key and direction.</returns>
    public static PaneSortStatus For(PaneSortOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return order.Key == SortKey.Name
            ? ByDirection(order, NameAscending, NameDescending)
            : order.Key == SortKey.Extension
                ? ByDirection(order, ExtensionAscending, ExtensionDescending)
                : order.Key == SortKey.Size
                    ? ByDirection(order, SizeAscending, SizeDescending)
                    : ByDirection(order, ModifiedAscending, ModifiedDescending);
    }

    private static PaneSortStatus ByDirection(
        PaneSortOrder order,
        PaneSortStatus ascending,
        PaneSortStatus descending)
    {
        return order.Direction == SortDirection.Ascending ? ascending : descending;
    }
}
