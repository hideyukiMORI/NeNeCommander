using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the remaining held-content and post-effect boundaries require their explicit owners before delegation.</summary>
[TestClass]
public sealed class WindowsTransferRequiredOwnerTests
{
    /// <summary>Post-effect observation cannot infer a lease when its caller supplies none.</summary>
    [TestMethod]
    public void ObserveAfterContentChangeWhenLeaseIsAbsentRejectsItsOwningParameter()
    {
        TransferArgumentAssertions.Rejects(() => WindowsOverwriteFileAdmission.ObserveAfterContentChange(null!), "lease");
    }

    /// <summary>The local step boundary diagnoses its missing drive owner before constructing a nested copy executor.</summary>
    [TestMethod]
    public void ConstructWhenLocalDriveOwnerIsAbsentRejectsAtStepBoundary()
    {
        WindowsDriveBoundary drives = new();
        WindowsOverwriteDependencies dependencies = WindowsOverwriteDependencies.Native(new DeterministicIdentifierSource());

        TransferArgumentAssertions.Rejects(() => _ = new WindowsLocalOperationSteps(new WindowsWslFileSystem(drives), null!, dependencies), "driveBoundary");
    }

    /// <summary>An absent source snapshot is refused before the provider can admit an original pair or allocate artifacts.</summary>
    [TestMethod]
    [DataRow("active")]
    [DataRow("cancelled")]
    public async Task CopyAsyncWhenSourceSnapshotIsAbsentDoesNotBeginAttemptAsync(string request)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        DeterministicIdentifierSource identifiers = new();
        WindowsOverwriteCopy copy = new(WindowsOverwriteDependencies.Native(identifiers), new WindowsDriveBoundary());

        using CancellationTokenSource cancellation = new();
        if (request == "cancelled")
        {
            cancellation.Cancel();
        }

        await TransferArgumentAssertions.RejectsAsync(() => copy.CopyAsync(null!, files.Pair.Evidence, cancellation.Token), "source");

        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.HasCount(2, Directory.GetFiles(files.Root.Path.CanonicalText));
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual("target", files.Read(files.Pair.Target.Path));
    }

    /// <summary>A missing held stream cannot be replaced by a permissive injected reader returning an empty file.</summary>
    [TestMethod]
    public async Task ReadHeldAsyncWhenHandleIsAbsentDoesNotAskInjectedReaderAsync()
    {
        int reads = 0;
        WindowsTransferContentIo io = new((_, _, _, _) =>
        {
            reads++;
            return ValueTask.FromResult(0);
        }, RandomAccess.WriteAsync, RandomAccess.SetLength, RandomAccess.FlushToDisk);

        await TransferArgumentAssertions.RejectsAsync(() => VerifiedFileContent.ReadHeldAsync(null!,
            (KnownEntrySize)EntrySize.Create(0), io, CancellationToken.None), "handle");

        Assert.AreEqual(0, reads);
    }

    /// <summary>Borrowing the very same handle twice cannot turn comparison or copying into a self-operation.</summary>
    [TestMethod]
    public void CreateWhenContentHandlesAreTheSameRejectsTargetWithoutTakingOwnership()
    {
        using WindowsTransferContentFixture files = WindowsTransferContentFixture.Create("source", "target", 6);

        ArgumentException failure = Assert.ThrowsExactly<ArgumentException>(() =>
            WindowsTransferContentPair.Create(files.Pair.Source, files.Pair.Source, files.Pair.Size));

        Assert.AreEqual("target", failure.ParamName);
        Assert.IsFalse(files.Pair.Source.IsClosed);
        Assert.IsFalse(files.Pair.Target.IsClosed);
        Assert.AreEqual("source", files.Read("source"));
        Assert.AreEqual("target", files.Read("target"));
    }
}
