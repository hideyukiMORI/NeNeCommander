using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.Wsl;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Hand-written catalogs that answer every read with one fixed outcome.</summary>
internal sealed class FixedLocationCatalogs : IDriveCatalog, IWslDistributionCatalog
{
    private readonly DriveCatalogOutcome _drives;
    private readonly WslDistributionCatalogOutcome _wslRoots;

    internal FixedLocationCatalogs(DriveCatalogOutcome drives, WslDistributionCatalogOutcome wslRoots)
    {
        _drives = drives;
        _wslRoots = wslRoots;
    }

    public Task<DriveCatalogOutcome> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_drives);
    }

    public Task<WslDistributionCatalogOutcome> DiscoverAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_wslRoots);
    }
}
