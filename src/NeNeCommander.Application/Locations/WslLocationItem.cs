using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Locations;

/// <summary>Represents one listed WSL distribution root in the Locations picker.</summary>
public sealed record WslLocationItem : LocationItem
{
    internal WslLocationItem(WslPath root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Root = root;
    }

    /// <summary>Gets the validated distribution root.</summary>
    public WslPath Root { get; }

    /// <inheritdoc />
    public override FileSystemPath Location => Root;
}
