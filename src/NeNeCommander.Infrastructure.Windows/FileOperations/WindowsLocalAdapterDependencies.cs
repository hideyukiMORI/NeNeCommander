using System;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Groups the local adapter's existing platform owners and the explicitly composed overwrite dependencies.</summary>
internal sealed class WindowsLocalAdapterDependencies
{
    internal WindowsLocalAdapterDependencies(WindowsRecycleBin recycleBin, IWslFileSystem fileSystem,
        WindowsDriveBoundary drives, WindowsOverwriteDependencies overwrite)
    {
        ArgumentNullException.ThrowIfNull(recycleBin);
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(drives);
        ArgumentNullException.ThrowIfNull(overwrite);
        RecycleBin = recycleBin;
        FileSystem = fileSystem;
        Drives = drives;
        Overwrite = overwrite;
    }

    internal WindowsRecycleBin RecycleBin { get; }
    internal IWslFileSystem FileSystem { get; }
    internal WindowsDriveBoundary Drives { get; }
    internal WindowsOverwriteDependencies Overwrite { get; }
}
