using System;
using System.IO;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Admits existing local destination ancestors for a WSL-to-Windows transfer.</summary>
internal static class WindowsLocalTransferDestination
{
    internal static FileOperationFailureKind? Failure(WindowsLocalPath destination)
    {
        return Failure(destination, new WindowsDriveBoundary());
    }

    internal static FileOperationFailureKind? Failure(WindowsLocalPath destination, WindowsDriveBoundary driveBoundary)
    {
        if (!AdmitsDrive(destination, driveBoundary))
        {
            return FileOperationFailureKind.ProviderUnavailable;
        }
        FileSystemPath? current = destination;
        while (current is WindowsLocalPath location)
        {
            DirectoryInfo directory = new(location.CanonicalText);
            if (!directory.Exists)
            {
                return FileOperationFailureKind.NotFound;
            }
            if (WindowsLocalTreeCopy.IsReparsePoint(directory))
            {
                return FileOperationFailureKind.ProviderUnavailable;
            }
            current = location.Parent;
        }
        return null;
    }

    internal static bool AdmitsDrive(WindowsLocalPath destination, WindowsDriveBoundary driveBoundary)
    {
        try
        {
            return driveBoundary.ReadType(destination) is DriveType.Fixed or DriveType.Removable;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    internal static bool Exists(WindowsLocalPath target)
    {
        return Exists(target.CanonicalText);
    }

    internal static bool Exists(string targetText)
    {
        return File.Exists(targetText) || Directory.Exists(targetText);
    }
}
