using System;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Panes;

/// <summary>Supplies validated read limits and the pane's preferences before its first listing.</summary>
public sealed record PaneSessionConfiguration
{
    private PaneSessionConfiguration(
        VisiblePageCapacity visiblePageCapacity,
        int entryBoundary,
        HiddenItemVisibility hiddenItemVisibility,
        PaneSortOrder sortOrder)
    {
        VisiblePageCapacity = visiblePageCapacity;
        EntryBoundary = entryBoundary;
        HiddenItemVisibility = hiddenItemVisibility;
        SortOrder = sortOrder;
    }

    /// <summary>Gets the validated row capacity.</summary>
    public VisiblePageCapacity VisiblePageCapacity { get; }

    /// <summary>Gets the bounded maximum number of entries requested.</summary>
    public int EntryBoundary { get; }

    /// <summary>Gets visibility used before the pane owns a listed state.</summary>
    public HiddenItemVisibility HiddenItemVisibility { get; }

    /// <summary>Gets the order used before the pane owns a listed state.</summary>
    public PaneSortOrder SortOrder { get; }

    /// <summary>Creates the complete startup configuration and validates its entry boundary.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The entry boundary is outside the fixed range.</exception>
    public static PaneSessionConfiguration Create(
        VisiblePageCapacity visiblePageCapacity,
        int entryBoundary,
        HiddenItemVisibility hiddenItemVisibility,
        PaneSortOrder sortOrder)
    {
        ArgumentNullException.ThrowIfNull(visiblePageCapacity);
        ArgumentNullException.ThrowIfNull(hiddenItemVisibility);
        ArgumentNullException.ThrowIfNull(sortOrder);
        return DirectoryReadRequest.IsValidEntryBoundary(entryBoundary)
            ? new PaneSessionConfiguration(visiblePageCapacity, entryBoundary, hiddenItemVisibility, sortOrder)
            : throw new ArgumentOutOfRangeException(nameof(entryBoundary));
    }
}
