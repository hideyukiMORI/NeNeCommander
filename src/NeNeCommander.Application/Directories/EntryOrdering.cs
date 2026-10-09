using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Owns the sole projection from listed entries and a pane sort order to the sequence a pane
/// shows (ADR-0053). Directories always precede files; inside each group the order's key and
/// direction decide. The projection is pure: it never changes the listing it reads, and the same
/// input always yields the same sequence.
/// </summary>
public static class EntryOrdering
{
    /// <summary>
    /// Projects entries into the order a pane shows them. The name comparison ignores case first
    /// and then compares ordinally, so providers with case-sensitive names stay deterministic. The
    /// extension is the text after the last dot of the name; a name without a dot, or whose only
    /// dot is its first character, has no extension and sorts first ascending. Extensions compare
    /// ignoring case and ties fall back to the ascending name comparison. Descending reverses only
    /// the key comparison, never the directory precedence.
    /// </summary>
    /// <param name="entries">Entries of one listing in any order.</param>
    /// <param name="order">Pane sort order to apply.</param>
    /// <returns>An owned read-only sequence holding exactly the given entries.</returns>
    public static IReadOnlyList<DirectoryEntry> Apply(IReadOnlyList<DirectoryEntry> entries, PaneSortOrder order)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(order);
        IComparer<DirectoryEntry> comparer = Comparer<DirectoryEntry>.Create(
            (left, right) => CompareWithinGroup(left, right, order));
        List<DirectoryEntry> ordered = [.. entries
            .OrderBy(entry => entry.Kind == DirectoryEntryKind.Directory ? 0 : 1)
            .ThenBy(entry => entry, comparer)];
        return ordered.AsReadOnly();
    }

    private static int CompareWithinGroup(DirectoryEntry left, DirectoryEntry right, PaneSortOrder order)
    {
        DirectoryEntry first = order.Direction == SortDirection.Descending ? right : left;
        DirectoryEntry second = order.Direction == SortDirection.Descending ? left : right;
        if (order.Key == SortKey.Name)
        {
            return CompareNames(first, second);
        }
        int byExtension = ExtensionOf(first.Name).CompareTo(
            ExtensionOf(second.Name),
            StringComparison.OrdinalIgnoreCase);
        return byExtension != 0 ? byExtension : CompareNames(left, right);
    }

    private static int CompareNames(DirectoryEntry left, DirectoryEntry right)
    {
        int ignoringCase = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        return ignoringCase != 0 ? ignoringCase : string.CompareOrdinal(left.Name, right.Name);
    }

    private static ReadOnlySpan<char> ExtensionOf(string name)
    {
        int dot = name.LastIndexOf('.');
        return dot <= 0 ? [] : name.AsSpan(dot + 1);
    }
}
