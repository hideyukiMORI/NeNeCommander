using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Owns a live source and restores only the exact recycle receipt created from it.</summary>
internal sealed class LiveWindowsRecycleFixture
{
    private readonly string _rootIdentity;
    private readonly string _sourceIdentity;
    private readonly List<string> _ownedEntries;

    internal LiveWindowsRecycleFixture(TestOwnedTemporaryRoot root, WindowsLocalPath source)
    {
        Root = root;
        Source = FileEntrySnapshot.Create(source,
            WindowsLocalEntryIdentity.Describe(WindowsLocalEntryIdentity.Find(source) ?? throw new InvalidOperationException("Missing owned source.")),
            DeletionCapability.Recycle);
        _rootIdentity = WindowsFileIdentifier.Describe(root.Path.CanonicalText);
        _sourceIdentity = WindowsFileIdentifier.Describe(source.CanonicalText);
        _ = root.WriteFile("recycle-owner.txt", "root=" + _rootIdentity + "\nsource=" + source.CanonicalText + "\nidentity=" + _sourceIdentity);
        _ownedEntries = [.. Directory.EnumerateFileSystemEntries(root.Path.CanonicalText, "*", SearchOption.AllDirectories)];
    }

    internal TestOwnedTemporaryRoot Root { get; }
    internal FileEntrySnapshot Source { get; }

    internal static TestOwnedTemporaryRoot OpenRoot(TestContext context)
    {
        _ = context.Properties.TryGetValue("NENE_COMMANDER_RECYCLE_TEST_ROOT", out object? configured);
        if (configured is not string)
        {
            Assert.Inconclusive("Live recycle requires an explicit owned D-drive temporary parent.");
        }
        WindowsLocalPath expected = WindowsRecycleBinTests.Path(Assert.IsInstanceOfType<string>(configured));
        Assert.AreEqual("D:", expected.Drive);
        Assert.IsNull(WindowsLocalTransferDestination.Failure(expected));
        TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(root.Path.Parent, expected));
        return root;
    }

    internal void Restore(IWindowsShellEntry receipt)
    {
        RequireRoot();
        Assert.IsFalse(File.Exists(Source.Path.CanonicalText) || Directory.Exists(Source.Path.CanonicalText));
        Assert.AreEqual(0, receipt.GetDisplayName(0x80058000, out string recycledPath));
        Assert.AreEqual(_sourceIdentity, WindowsFileIdentifier.Describe(recycledPath));
        _ = Root.WriteFile("recycle-receipt.txt", recycledPath);
        _ownedEntries.Add(Root.Resolve("recycle-receipt.txt"));
        WindowsRecycleShell shell = new();
        using WindowsShellEntry destination = (WindowsShellEntry)shell.OpenEntry(Root.Path);
        using WindowsShellOperation operation = (WindowsShellOperation)shell.OpenOperation();
        uint flags = (WindowsRecycleSession.OperationFlags & ~0x00080000u) | 0x8u;
        Assert.AreEqual(0, operation.SetOperationFlags(flags));
        RequireRoot();
        Assert.AreEqual(0, operation.Native.MoveItem(((WindowsShellEntry)receipt).Native, destination.Native,
            System.IO.Path.GetFileName(Source.Path.CanonicalText), null));
        Assert.AreEqual(0, operation.PerformOperations());
        Assert.AreEqual(0, operation.GetAnyOperationsAborted(out int aborted));
        Assert.AreEqual(0, aborted);
        Assert.AreEqual(_sourceIdentity, WindowsFileIdentifier.Describe(Source.Path.CanonicalText));
        RequireRoot();
    }

    internal void RequireRoot()
    {
        Assert.IsNull(WindowsLocalTransferDestination.Failure(Root.Path));
        Assert.AreEqual(_rootIdentity, WindowsFileIdentifier.Describe(Root.Path.CanonicalText));
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(Source.Path.Parent, Root.Path));
    }

    internal void Cleanup()
    {
        RequireRoot();
        Assert.AreEqual(_sourceIdentity, WindowsFileIdentifier.Describe(Source.Path.CanonicalText));
        Assert.IsFalse(WindowsLocalTreeCopy.ContainsReparsePoint(new DirectoryInfo(Root.Path.CanonicalText)));
        CollectionAssert.AreEquivalent(_ownedEntries,
            Directory.EnumerateFileSystemEntries(Root.Path.CanonicalText, "*", SearchOption.AllDirectories).ToList());
        Root.Dispose();
    }
}
