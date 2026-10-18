using System;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Projects location, activity, ordering and observed-drive indications for one pane.</summary>
internal sealed record PaneHeaderPresentation
{
    private PaneHeaderPresentation(
        PaneStatus status,
        string addressText,
        PaneSortStatus? sortStatus,
        PaneLocationStatus? locationStatus)
    {
        Status = status;
        AddressText = addressText;
        SortStatus = sortStatus;
        LocationStatus = locationStatus;
    }

    internal PaneStatus Status { get; }
    internal string AddressText { get; }
    internal PaneSortStatus? SortStatus { get; }
    internal PaneLocationStatus? LocationStatus { get; }

    internal static PaneHeaderPresentation Create(PaneSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.Content is PaneContentListed listed
            ? new PaneHeaderPresentation(
                PaneActivityStatusPresenter.Present(snapshot.Activity, TranslateListing(listed.Listing)),
                listed.Listing.Location.CanonicalText,
                PaneSortStatus.For(listed.State.SortOrder),
                listed.DriveKind == DriveKind.Network ? PaneLocationStatus.NetworkDrive : null)
            : new PaneHeaderPresentation(
                PaneActivityStatusPresenter.Present(snapshot.Activity, PaneStatus.NoListing),
                TargetText(snapshot.Activity), null, null);
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
            PaneReadAbandoned abandoned => abandoned.Target.CanonicalText,
            PaneReadFailed failed => failed.Target.CanonicalText,
            _ => string.Empty,
        };
    }
}
