using System;
using System.Collections.Generic;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Projects one pane snapshot onto a deterministic presentation of rows, focus, status, and address.
/// </summary>
public static class PaneListingPresenter
{
    /// <summary>
    /// Translates a snapshot into render-ready values without changing any state.
    /// </summary>
    /// <param name="snapshot">Current pane snapshot.</param>
    /// <returns>A render-ready presentation.</returns>
    public static PanePresentation Present(PaneSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.Content is PaneContentListed listed
            ? PresentListed(listed, snapshot.Activity)
            : new PanePresentation(
                Array.Empty<PaneRow>(),
                null,
                TranslateActivity(snapshot.Activity, PaneStatus.NoListing),
                TargetText(snapshot.Activity));
    }

    private static PanePresentation PresentListed(PaneContentListed listed, PaneActivity activity)
    {
        HashSet<FileSystemPath> selection = new(listed.State.Selection, FileSystemPathIdentityComparer.Instance);
        List<PaneRow> rows = [];
        PaneRow? focusRow = null;
        foreach (DirectoryEntry entry in listed.Listing.Entries)
        {
            PaneRow row = new(entry, selection.Contains(entry.Path) ? PaneRowMark.Selected : PaneRowMark.Unselected);
            rows.Add(row);
            if (FileSystemPathIdentityComparer.Instance.Equals(entry.Path, listed.State.FocusItem))
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
