using System;
using System.IO;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Scripts one path's drive type behind the existing <see cref="WindowsDriveBoundary"/> seam: the path is
/// fixed for its first admitted queries and then reports another type, modelling a drive remapped between
/// an earlier admission and a later recheck. Every other path is fixed.
/// </summary>
internal sealed class ScriptedDriveTypes
{
    private readonly string _path;
    private readonly int _admittedQueries;
    private readonly DriveType _laterType;

    internal ScriptedDriveTypes(string path, int admittedQueries, DriveType laterType = DriveType.Network)
    {
        _path = path;
        _admittedQueries = admittedQueries;
        _laterType = laterType;
    }

    /// <summary>Gets how many times the scripted path was queried.</summary>
    internal int Queries { get; private set; }

    internal WindowsDriveBoundary Boundary()
    {
        return new WindowsDriveBoundary(Read);
    }

    private DriveType Read(WindowsLocalPath path)
    {
        if (!path.CanonicalText.Equals(_path, StringComparison.OrdinalIgnoreCase))
        {
            return DriveType.Fixed;
        }
        Queries++;
        return Queries > _admittedQueries ? _laterType : DriveType.Fixed;
    }
}
