using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Shares the existing deterministic setup and assertions for WslFileOperationAdapterTests.</summary>
internal static class WslOperationTestContext
{
    internal static void AssertEffect(
        FileOperationOutcome outcome,
        FileOperationEffectKind expectedKind,
        FileSystemPath expectedSource)
    {
        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(expectedKind, outcome.Effects[0].Kind);
        Assert.AreEqual(expectedSource, outcome.Effects[0].Source);
    }

    internal static WslFileOperationAdapter Adapter(ScriptedWslFileSystem fileSystem)
    {
        return new WslFileOperationAdapter(new WindowsLocalIoExecutionBoundary(), fileSystem);
    }

    internal static WslFileSystemEntry Entry(
        WslPath path,
        string identity,
        DirectoryEntryKind kind,
        FileAttributes attributes = FileAttributes.None)
    {
        return ScriptedWslFileSystem.Entry(path, identity, kind, attributes);

    }

    internal static FileEntrySnapshot Snapshot(WslFileSystemEntry entry)
    {
        return FileEntrySnapshot.Create(entry.Path, entry.Identity, DeletionCapability.PermanentOnly);
    }

    internal static WslPath Wsl(string text)
    {
        return Assert.IsInstanceOfType<WslPath>(Path(text));
    }

    internal static FileSystemPath Local(string text)
    {
        return Path(text);
    }

    internal static FileSystemPath Path(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
