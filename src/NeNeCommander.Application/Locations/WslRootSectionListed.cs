using System;
using System.Collections.Generic;
using NeNeCommander.Application.Wsl;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Locations;

/// <summary>Represents a WSL section whose discovery succeeded, in provider order.</summary>
public sealed record WslRootSectionListed : WslRootSection
{
    internal WslRootSectionListed(WslDistributionCatalogSucceeded discovery)
    {
        ArgumentNullException.ThrowIfNull(discovery);
        List<WslLocationItem> items = new(discovery.Roots.Count);
        foreach (WslPath root in discovery.Roots)
        {
            items.Add(new WslLocationItem(root));
        }
        Items = items.AsReadOnly();
    }

    /// <summary>Gets the listed distribution entries in provider order.</summary>
    public IReadOnlyList<WslLocationItem> Items { get; }
}
