using System;
using System.IO;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.Directories;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class WindowsTransferContentFixture : IDisposable
{
    private WindowsTransferContentFixture(TestOwnedTemporaryRoot root, SafeFileHandle source, SafeFileHandle target, KnownEntrySize size)
    {
        Root = root;
        Pair = WindowsTransferContentPair.Create(source, target, size);
    }

    internal TestOwnedTemporaryRoot Root { get; }
    internal WindowsTransferContentPair Pair { get; }
    internal string SourcePath => Root.Resolve("source");
    internal string TargetPath => Root.Resolve("target");

    internal static WindowsTransferContentFixture Create(string source, string target, long expectedLength)
    {
        TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        SafeFileHandle? sourceHandle = null;
        SafeFileHandle? targetHandle = null;
        bool completed = false;
        try
        {
            string sourcePath = root.WriteFile("source", source);
            string targetPath = root.WriteFile("target", target);
            sourceHandle = File.OpenHandle(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            targetHandle = File.OpenHandle(targetPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
            WindowsTransferContentFixture fixture = new(root, sourceHandle, targetHandle, (KnownEntrySize)EntrySize.Create(expectedLength));
            sourceHandle = null;
            targetHandle = null;
            completed = true;
            return fixture;
        }
        finally
        {
            targetHandle?.Dispose();
            sourceHandle?.Dispose();
            if (!completed)
            {
                root.Dispose();
            }
        }
    }

    internal string Read(string relativePath)
    {
        using FileStream stream = new(Root.Resolve(relativePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    public void Dispose()
    {
        Pair.Target.Dispose();
        Pair.Source.Dispose();
        Root.Dispose();
    }
}
