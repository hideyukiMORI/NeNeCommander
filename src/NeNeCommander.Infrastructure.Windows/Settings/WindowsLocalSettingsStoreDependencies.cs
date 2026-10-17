using System;
using NeNeCommander.Application.Identifiers;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>Groups the settings store's composed dependencies without adding another persistence route.</summary>
internal sealed record WindowsLocalSettingsStoreDependencies
{
    internal WindowsLocalSettingsStoreDependencies(
        IIdentifierSource identifierSource,
        ISettingsWriteTestHook writeTestHook,
        WindowsDriveBoundary driveBoundary)
    {
        ArgumentNullException.ThrowIfNull(identifierSource);
        ArgumentNullException.ThrowIfNull(writeTestHook);
        ArgumentNullException.ThrowIfNull(driveBoundary);
        IdentifierSource = identifierSource;
        WriteTestHook = writeTestHook;
        DriveBoundary = driveBoundary;
    }

    internal IIdentifierSource IdentifierSource { get; }

    internal ISettingsWriteTestHook WriteTestHook { get; }

    internal WindowsDriveBoundary DriveBoundary { get; }
}
