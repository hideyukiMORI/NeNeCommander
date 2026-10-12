using System.IO;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Admits existing local destination ancestors for a WSL-to-Windows transfer.</summary>
internal static class WindowsLocalTransferDestination
{
    internal static FileOperationFailureKind? Failure(WindowsLocalPath destination)
    {
        DriveType driveType = new DriveInfo(destination.Drive).DriveType;
        if (driveType is not DriveType.Fixed and not DriveType.Removable)
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

    internal static bool Exists(WindowsLocalPath target)
    {
        return File.Exists(target.CanonicalText) || Directory.Exists(target.CanonicalText);
    }
}
