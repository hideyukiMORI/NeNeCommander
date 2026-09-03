using System;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Projects one closed directory read outcome onto a deterministic pane presentation.
/// </summary>
public static class PaneListingPresenter
{
    /// <summary>
    /// Translates a read outcome into rows, initial focus, and a status resource key.
    /// </summary>
    /// <param name="outcome">Closed outcome returned by the directory read port.</param>
    /// <returns>A render-ready presentation.</returns>
    public static PanePresentation Present(DirectoryReadOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return outcome switch
        {
            DirectoryReadSucceeded succeeded => PresentListing(succeeded.Listing),
            DirectoryReadCancelled => new PaneListingUnavailable(PaneStatus.Cancelled),
            DirectoryReadFailed failed => new PaneListingUnavailable(TranslateFailure(failed.Failure)),
            _ => throw new InvalidOperationException("The directory read outcome variant is not presentable."),
        };
    }

    private static PaneListingPresented PresentListing(DirectoryListing listing)
    {
        PaneStatus status = listing.Completeness == DirectoryListingCompleteness.Bounded
            ? PaneStatus.Bounded
            : listing.UnrepresentableEntryCount > 0 ? PaneStatus.EntriesOmitted : PaneStatus.Complete;
        DirectoryEntry? focusEntry = listing.Entries.Count == 0 ? null : listing.Entries[0];
        return new PaneListingPresented(listing, status, focusEntry);
    }

    private static PaneStatus TranslateFailure(FileOperationFailureKind failure)
    {
        return failure == FileOperationFailureKind.AccessDenied
            ? PaneStatus.AccessDenied
            : failure == FileOperationFailureKind.NotFound ? PaneStatus.NotFound : PaneStatus.ProviderUnavailable;
    }
}
