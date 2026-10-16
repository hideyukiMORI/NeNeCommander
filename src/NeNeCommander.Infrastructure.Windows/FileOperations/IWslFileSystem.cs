using System.IO;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns direct Windows-side access to one validated WSL namespace.</summary>
internal interface IWslFileSystem
{
    internal string ResolveVerificationPath(WslPath path);

    internal VerifiedEntrySnapshot CaptureVerificationEntry(WslPath path);

    internal CopyVerificationOutcome VerifyCopy(FileEntrySnapshot source, FileSystemPath target);

    internal ProviderStepOutcome DeleteVerifiedSource(FileEntrySnapshot source, VerifiedCopyReceipt receipt);

    internal WslFileSystemEntry? Find(WslPath path);

    internal bool TargetExists(WslPath path);

    internal bool ContainsReparsePoint(WslFileSystemEntry source);

    internal bool ContainsReparsePoint(WslPath target);

    internal ProviderStepOutcome Copy(WslFileSystemEntry source, WslPath target);

    internal ProviderStepOutcome CopyFromWindowsLocal(FileSystemInfo source, WslPath target);

    internal FileOperationFailureKind? PreflightToWindowsLocal(WslFileSystemEntry source, WindowsLocalPath target);

    internal ProviderStepOutcome CopyToWindowsLocal(WslFileSystemEntry source, WindowsLocalPath target);

    internal void CreateDirectory(WslPath target);

    internal void Rename(WslFileSystemEntry source, WslPath target);

    internal void Delete(WslFileSystemEntry source);
}
