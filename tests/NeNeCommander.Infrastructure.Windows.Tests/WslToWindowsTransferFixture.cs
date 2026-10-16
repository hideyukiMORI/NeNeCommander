using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Owns the local destination and mapped WSL source of reverse-transfer tests.</summary>
internal sealed class WslToWindowsTransferFixture : IDisposable
{
    private WslToWindowsTransferFixture(TestOwnedTemporaryRoot root, WindowsWslFileSystem fileSystem)
    {
        Root = root;
        FileSystem = fileSystem;
        WindowsLocalIoExecutionBoundary execution = new();
        Transfer = new WslToWindowsTransfer(execution, fileSystem);
        Router = new ProviderFileOperationPort(
            new WindowsLocalFileOperationAdapter(execution, fileSystem),
            new WslFileOperationAdapter(execution, fileSystem),
            new WindowsToWslTransfer(execution, fileSystem),
            Transfer);
        Destination = Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(root.Resolve("windows"))).Path);
    }

    internal TestOwnedTemporaryRoot Root { get; }
    internal WindowsWslFileSystem FileSystem { get; }
    internal WslToWindowsTransfer Transfer { get; }
    internal ProviderFileOperationPort Router { get; }
    internal WindowsLocalPath Destination { get; }

    internal static WslToWindowsTransferFixture Create(Func<string, WslHandleFacts>? readFacts = null)
    {
        TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("wsl");
        _ = root.CreateDirectory("windows");
        WindowsWslFileSystem fileSystem = new(
            path => OwnedWslNamespace.Resolve(root.ResolveVerbatim("wsl"), path),
            readFacts ?? OwnedWslNamespace.ReadFacts);
        return new WslToWindowsTransferFixture(root, fileSystem);
    }

    internal static WslPath Source(string name)
    {
        return Assert.IsInstanceOfType<WslPath>(Assert.IsInstanceOfType<PathParseSuccess>(
            FileSystemPath.Parse("\\\\wsl.localhost\\Ubuntu\\owned\\" + name)).Path);
    }

    internal CopyRequest Copy(params FileSystemPath[] sources)
    {
        return Assert.IsInstanceOfType<CopyRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            CopyRequest.Create(sources, Destination)).Request);
    }

    internal MoveRequest Move(params FileSystemPath[] sources)
    {
        return Assert.IsInstanceOfType<MoveRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            MoveRequest.Create(sources, Destination)).Request);
    }

    internal static void AssertEffects(FileOperationOutcome outcome, params FileOperationEffectKind[] expected)
    {
        Assert.HasCount(expected.Length, outcome.Effects);
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.AreSame(expected[index], outcome.Effects[index].Kind);
        }
    }

    public void Dispose()
    {
        Root.Dispose();
    }
}
