using System;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Normalizes pane activity through the one shared presentation status mapping.</summary>
internal static class PaneActivityStatusPresenter
{
    internal static PaneStatus Present(PaneActivity activity, PaneStatus idleStatus)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(idleStatus);
        return activity switch
        {
            PaneLoading => PaneStatus.Loading,
            PaneReadCancelled => PaneStatus.Cancelled,
            PaneReadFailed failed => PresentFailure(failed.Failure),
            _ => idleStatus,
        };
    }

    private static PaneStatus PresentFailure(FileOperationFailureKind failure)
    {
        return failure == FileOperationFailureKind.AccessDenied
            ? PaneStatus.AccessDenied
            : failure == FileOperationFailureKind.NotFound
                ? PaneStatus.NotFound
                : PaneStatus.ProviderUnavailable;
    }
}
