using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns direct Windows-side access to one validated WSL namespace.</summary>
internal interface IWslFileSystem
{
    internal WslFileSystemEntry? Find(WslPath path);

    internal bool TargetExists(WslPath path);

    internal bool ContainsReparsePoint(WslFileSystemEntry source);

    internal bool ContainsReparsePoint(WslPath target);

    internal void Copy(WslFileSystemEntry source, WslPath target);

    internal bool Matches(WslFileSystemEntry source, WslPath target);

    internal void CreateDirectory(WslPath target);

    internal void Rename(WslFileSystemEntry source, WslPath target);

    internal void Delete(WslFileSystemEntry source);
}
