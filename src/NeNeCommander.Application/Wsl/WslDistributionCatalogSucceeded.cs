using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Wsl;

/// <summary>Represents one complete snapshot of registered WSL distribution roots.</summary>
public sealed record WslDistributionCatalogSucceeded : WslDistributionCatalogOutcome
{
    private readonly ReadOnlyCollection<WslPath> _roots;

    internal WslDistributionCatalogSucceeded(IReadOnlyList<WslPath> roots)
    {
        ArgumentNullException.ThrowIfNull(roots);
        List<WslPath> snapshot = new(roots.Count);
        HashSet<string> identities = new(StringComparer.OrdinalIgnoreCase);
        foreach (WslPath root in roots)
        {
            ArgumentNullException.ThrowIfNull(root);
            if (root.LinuxPath.Length != 1 || !identities.Add(root.DistributionName))
            {
                throw new ArgumentException("Distribution roots must be unique WSL roots.", nameof(roots));
            }
            snapshot.Add(root);
        }
        _roots = snapshot.AsReadOnly();
    }

    /// <summary>Gets the validated owned snapshot in provider order.</summary>
    public IReadOnlyList<WslPath> Roots => _roots;
}
