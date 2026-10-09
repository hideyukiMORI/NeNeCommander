using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Projects one pane snapshot onto a deterministic presentation of rows, focus, status, and
/// address. Rows come from the pane state's visible set, which the reducer alone decides, so the
/// projection never re-reads the listing to work out what a pane shows.
/// </summary>
public static class PaneListingPresenter
{
    /// <summary>
    /// Translates a snapshot into render-ready values without changing any state.
    /// </summary>
    /// <param name="snapshot">Current pane snapshot.</param>
    /// <param name="frame">
    /// The pane's activation frame. The focus item is marked differently in the pane that receives
    /// intents than in the pane that only keeps its display, so the projection needs it.
    /// </param>
    /// <param name="format">
    /// Metadata format the host resolved once; the rows' size and modification texts come from it.
    /// </param>
    /// <returns>A render-ready presentation.</returns>
    public static PanePresentation Present(PaneSnapshot snapshot, PaneFrame frame, EntryMetadataFormat format)
    {
        return PresentCore(snapshot, frame, format, null);
    }

    internal static PanePresentation Present(
        PaneSnapshot snapshot,
        PaneFrame frame,
        EntryMetadataFormat format,
        PanePresentation previous)
    {
        ArgumentNullException.ThrowIfNull(previous);
        return PresentCore(snapshot, frame, format, previous);
    }

    private static PanePresentation PresentCore(
        PaneSnapshot snapshot,
        PaneFrame frame,
        EntryMetadataFormat format,
        PanePresentation? previous)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(format);
        PanePresentation? reusable = previous is not null && ReferenceEquals(previous.OwnedRows.Format, format)
            ? previous
            : null;
        return reusable is not null &&
            ReferenceEquals(reusable.SourceSnapshot, snapshot) &&
            reusable.SourceFrame == frame
                ? reusable
                : snapshot.Content is not PaneContentListed listed
                    ? PresentUnlisted(snapshot, frame, format, reusable)
                    : reusable is not null && CanReuseRows(listed, reusable)
                        ? UpdateListed(snapshot, listed, frame, reusable)
                        : CreateListed(snapshot, listed, frame, format);
    }

    private static PanePresentation PresentUnlisted(
        PaneSnapshot snapshot,
        PaneFrame frame,
        EntryMetadataFormat format,
        PanePresentation? previous)
    {
        PaneRows rows = previous is not null && previous.Rows.Count == 0
            ? previous.OwnedRows
            : new PaneRows(Array.Empty<PaneRow>(), format);
        return new PanePresentation(
            rows,
            null,
            PaneActivityStatusPresenter.Present(snapshot.Activity, PaneStatus.NoListing),
            TargetText(snapshot.Activity),
            snapshot,
            frame);
    }

    /// <summary>
    /// Decides whether the previous rows still hold the visible set in its order. The same
    /// listing with the same visible count holds the same entries, because every visible set is a
    /// subset of that listing; the same sort order then places them in the same order. A new
    /// order rebuilds every row (ADR-0053). The caller has already rejected rows projected with
    /// another metadata format, so the same entries also still have the same size and
    /// modification texts.
    /// </summary>
    private static bool CanReuseRows(PaneContentListed listed, PanePresentation previous)
    {
        return previous.SourceSnapshot.Content is PaneContentListed prior &&
            ReferenceEquals(prior.Listing, listed.Listing) &&
            prior.State.SortOrder == listed.State.SortOrder &&
            previous.Rows.Count == listed.State.VisibleEntries.Count;
    }

    private static PanePresentation CreateListed(
        PaneSnapshot snapshot,
        PaneContentListed listed,
        PaneFrame frame,
        EntryMetadataFormat format)
    {
        HashSet<FileSystemPath> selection = new(listed.State.Selection, FileSystemPathIdentityComparer.Instance);
        List<PaneRow> rows = [];
        foreach (DirectoryEntry entry in listed.State.VisibleEntries)
        {
            rows.Add(new PaneRow(entry, ResolveMark(entry, listed, selection, frame), format));
        }
        PaneRows ownedRows = new(rows, format);
        return new PanePresentation(
            ownedRows,
            FindFocusRow(ownedRows, listed.State.FocusItem),
            PaneActivityStatusPresenter.Present(snapshot.Activity, TranslateListing(listed.Listing)),
            listed.Listing.Location.CanonicalText,
            snapshot,
            frame);
    }

    private static PanePresentation UpdateListed(
        PaneSnapshot snapshot,
        PaneContentListed listed,
        PaneFrame frame,
        PanePresentation previous)
    {
        PaneContentListed prior = (PaneContentListed)previous.SourceSnapshot.Content;
        HashSet<FileSystemPath> priorSelection = new(
            prior.State.Selection,
            FileSystemPathIdentityComparer.Instance);
        HashSet<FileSystemPath> selection = new(
            listed.State.Selection,
            FileSystemPathIdentityComparer.Instance);
        HashSet<FileSystemPath> affected = new(FileSystemPathIdentityComparer.Instance);
        AddWhenPresent(affected, prior.State.FocusItem);
        AddWhenPresent(affected, listed.State.FocusItem);
        affected.UnionWith(priorSelection.Where(path => !selection.Contains(path)));
        affected.UnionWith(selection.Where(path => !priorSelection.Contains(path)));

        PaneRows rows = previous.OwnedRows;
        IEnumerable<int> affectedIndexes = affected
            .Select(path => rows.TryGetIndex(path, out int index) ? index : -1)
            .Where(index => index >= 0);
        IEnumerable<(int Index, PaneRow Current, PaneRowMark Mark)> changedRows = affectedIndexes
            .Select(index => (Index: index, Current: rows[index]))
            .Select(row => (
                row.Index,
                row.Current,
                Mark: ResolveMark(row.Current.Entry, listed, selection, frame)))
            .Where(row => row.Current.Mark != row.Mark);
        foreach ((int index, PaneRow current, PaneRowMark mark) in changedRows)
        {
            rows.Replace(index, current.WithMark(mark));
        }

        return new PanePresentation(
            rows,
            FindFocusRow(rows, listed.State.FocusItem),
            PaneActivityStatusPresenter.Present(snapshot.Activity, TranslateListing(listed.Listing)),
            listed.Listing.Location.CanonicalText,
            snapshot,
            frame);
    }

    private static void AddWhenPresent(HashSet<FileSystemPath> paths, FileSystemPath? path)
    {
        if (path is not null)
        {
            _ = paths.Add(path);
        }
    }

    private static PaneRow? FindFocusRow(PaneRows rows, FileSystemPath? focus)
    {
        return focus is not null && rows.TryGetIndex(focus, out int index) ? rows[index] : null;
    }

    /// <summary>
    /// Resolves the single mark of one row. The focus item of the active pane outranks selection,
    /// selection outranks the focus item of the passive pane, and every other row is unmarked.
    /// </summary>
    private static PaneRowMark ResolveMark(
        DirectoryEntry entry,
        PaneContentListed listed,
        HashSet<FileSystemPath> selection,
        PaneFrame frame)
    {
        return HasFocus(entry, listed) && frame == PaneFrame.Active
            ? PaneRowMark.FocusInActivePane
            : selection.Contains(entry.Path)
                ? PaneRowMark.Selected
                : HasFocus(entry, listed) ? PaneRowMark.FocusInPassivePane : PaneRowMark.Unmarked;
    }

    private static bool HasFocus(DirectoryEntry entry, PaneContentListed listed)
    {
        return FileSystemPathIdentityComparer.Instance.Equals(entry.Path, listed.State.FocusItem);
    }

    private static PaneStatus TranslateListing(DirectoryListing listing)
    {
        return listing.Completeness == DirectoryListingCompleteness.Bounded
            ? PaneStatus.Bounded
            : listing.UnrepresentableEntryCount > 0 ? PaneStatus.EntriesOmitted : PaneStatus.Complete;
    }

    private static string TargetText(PaneActivity activity)
    {
        return activity switch
        {
            PaneLoading loading => loading.Target.CanonicalText,
            PaneReadCancelled cancelled => cancelled.Target.CanonicalText,
            PaneReadFailed failed => failed.Target.CanonicalText,
            _ => string.Empty,
        };
    }

}
