using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Drives;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Hand-written drive catalog for sessions whose tests never open the Locations picker.</summary>
internal sealed class PendingDriveCatalog : IDriveCatalog
{
    public Task<DriveCatalogOutcome> ListAsync(CancellationToken cancellationToken)
    {
        return new TaskCompletionSource<DriveCatalogOutcome>().Task;
    }
}
