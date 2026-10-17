using System;
using System.IO;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Drives;

/// <summary>Reads the current OS drive type for a typed local path at each admission boundary.</summary>
internal sealed class WindowsDriveBoundary
{
    private readonly Func<WindowsLocalPath, DriveType> _readType;

    internal WindowsDriveBoundary()
        : this(path => new DriveInfo(path.Drive).DriveType)
    {
    }

    internal WindowsDriveBoundary(Func<WindowsLocalPath, DriveType> readType)
    {
        ArgumentNullException.ThrowIfNull(readType);
        _readType = readType;
    }

    internal DriveType ReadType(WindowsLocalPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return _readType(path);
    }

    internal bool AdmitsLocal(WindowsLocalPath path)
    {
        try
        {
            return ReadType(path) is DriveType.Fixed or DriveType.Removable or DriveType.CDRom or DriveType.Ram;
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
}
