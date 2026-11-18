using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.Wsl;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Builds canonical drive and WSL catalog outcomes for Locations session and model tests.</summary>
internal static class LocationsCatalogTestData
{
    internal static DriveLocation Drive(string root, DriveKind kind, string? label)
    {
        return DriveLocation.Create(Assert.IsInstanceOfType<WindowsLocalPath>(Parse(root)), kind, label);
    }

    internal static DriveCatalogOutcome DrivesOf(params DriveLocation[] drives)
    {
        return DriveCatalogOutcome.Succeeded(drives, 0);
    }

    internal static WslDistributionCatalogOutcome RootsOf(params string[] names)
    {
        WslPath[] roots = new WslPath[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            roots[index] = Assert.IsInstanceOfType<WslPath>(Parse("\\\\wsl.localhost\\" + names[index]));
        }
        return WslDistributionCatalogOutcome.Succeeded(roots);
    }

    private static FileSystemPath Parse(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
