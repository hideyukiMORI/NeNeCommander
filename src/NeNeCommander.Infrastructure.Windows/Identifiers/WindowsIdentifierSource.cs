using System;
using NeNeCommander.Application.Identifiers;

namespace NeNeCommander.Infrastructure.Windows.Identifiers;

/// <summary>Supplies attempt tokens at the sole production identifier boundary.</summary>
public sealed class WindowsIdentifierSource : IIdentifierSource
{
    /// <inheritdoc />
    public Guid NextIdentifier()
    {
        return Guid.NewGuid();
    }
}
