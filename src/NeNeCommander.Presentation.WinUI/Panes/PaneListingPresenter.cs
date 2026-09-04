using System;
using System.Collections.Generic;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
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
    /// <returns>A render-ready presentation.</returns>
    public static PanePresentation Present(PaneSnapshot snapshot, PaneFrame frame)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(frame);
        return snapshot.Content is PaneContentListed listed
            ? PresentListed(listed, snapshot.Activity, frame)
            : new PanePresentation(
                Array.Empty<PaneRow>(),
                null,
                TranslateActivity(snapshot.Activity, PaneStatus.NoListing),
                TargetText(snapshot.Activity));
    }

    private static PanePresentation PresentListed(PaneContentListed listed, PaneActivity activity, PaneFrame frame)
    {
        HashSet<FileSystemPath> selection = new(listed.State.Selection, FileSystemPathIdentityComparer.Instance);
        List<PaneRow> rows = [];
        PaneRow? focusRow = null;
        foreach (DirectoryEntry entry in listed.State.VisibleEntries)
        {
            PaneRow row = new(
                entry,
                ResolveMark(entry, listed, selection, frame),
                PaneRowKind.For(entry.Kind),
                PaneRowVisibility.For(entry.Visibility));
            rows.Add(row);
            if (HasFocus(entry, listed))
            {
                focusRow = row;
            }
        }
        return new PanePresentation(
            rows.AsReadOnly(),
            focusRow,
            TranslateActivity(activity, TranslateListing(listed.Listing)),
            listed.Listing.Location.CanonicalText);
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

    private static PaneStatus TranslateActivity(PaneActivity activity, PaneStatus idleStatus)
    {
        return activity switch
        {
            PaneLoading => PaneStatus.Loading,
            PaneReadCancelled => PaneStatus.Cancelled,
            PaneReadFailed failed => TranslateFailure(failed.Failure),
            _ => idleStatus,
        };
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

    private static PaneStatus TranslateFailure(FileOperationFailureKind failure)
    {
        return failure == FileOperationFailureKind.AccessDenied
            ? PaneStatus.AccessDenied
            : failure == FileOperationFailureKind.NotFound ? PaneStatus.NotFound : PaneStatus.ProviderUnavailable;
    }
}
