using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Wsl;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Wsl;

/// <summary>Discovers WSL distribution roots through the single fixed process boundary.</summary>
public sealed class WslDistributionCatalog : IWslDistributionCatalog
{
    internal const int OutputCharacterBoundary = 65536;
    internal const int DistributionBoundary = 256;

    private readonly IWslDistributionProcess _process;

    /// <summary>Initializes the catalog with the production WSL process boundary.</summary>
    public WslDistributionCatalog()
        : this(new WslDistributionProcess())
    {
    }

    internal WslDistributionCatalog(IWslDistributionProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);
        _process = process;
    }

    /// <inheritdoc />
    public async Task<WslDistributionCatalogOutcome> DiscoverAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return WslDistributionCatalogOutcome.Cancelled();
        }

        try
        {
            WslDistributionProcessResult result = await _process.ListAsync(cancellationToken);
            return result.ExitCode == 0
                ? ParseOutput(result.StandardOutput)
                : WslDistributionCatalogOutcome.Failed(WslDistributionCatalogFailureKind.ProviderUnavailable);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return WslDistributionCatalogOutcome.Cancelled();
        }
        catch (Win32Exception)
        {
            return WslDistributionCatalogOutcome.Failed(WslDistributionCatalogFailureKind.ProviderUnavailable);
        }
        catch (InvalidDataException)
        {
            return WslDistributionCatalogOutcome.Failed(WslDistributionCatalogFailureKind.MalformedOutput);
        }
        catch (IOException)
        {
            return WslDistributionCatalogOutcome.Failed(WslDistributionCatalogFailureKind.ProviderUnavailable);
        }
    }

    internal static WslDistributionCatalogOutcome ParseOutput(string output)
    {
        ArgumentNullException.ThrowIfNull(output);
        List<WslPath> roots = [];
        HashSet<string> identities = new(StringComparer.OrdinalIgnoreCase);
        string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length > DistributionBoundary)
        {
            return WslDistributionCatalogOutcome.Failed(WslDistributionCatalogFailureKind.MalformedOutput);
        }

        foreach (string line in lines)
        {
            PathParseOutcome parsed = FileSystemPath.Parse("\\\\wsl.localhost\\" + line);
            if (parsed is not PathParseSuccess { Path: WslPath root } || root.LinuxPath.Length != 1)
            {
                return WslDistributionCatalogOutcome.Failed(WslDistributionCatalogFailureKind.MalformedOutput);
            }
            if (identities.Add(root.DistributionName))
            {
                roots.Add(root);
            }
        }
        return WslDistributionCatalogOutcome.Succeeded(roots);
    }
}
