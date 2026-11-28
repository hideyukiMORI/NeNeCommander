using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves incomplete transfer composition is rejected before an operation can acquire or mutate files.</summary>
[TestClass]
public sealed class WindowsTransferDependencyAdmissionTests
{
    /// <summary>Held content access must supply every read, write, truncation and flush owner.</summary>
    [TestMethod]
    public void ConstructWhenContentDelegateIsAbsentRejectsTheMissingOperation()
    {
        TransferArgumentAssertions.Rejects(() => _ = new WindowsTransferContentIo(null!, RandomAccess.WriteAsync, RandomAccess.SetLength, RandomAccess.FlushToDisk), "read");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsTransferContentIo(RandomAccess.ReadAsync, null!, RandomAccess.SetLength, RandomAccess.FlushToDisk), "write");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsTransferContentIo(RandomAccess.ReadAsync, RandomAccess.WriteAsync, null!, RandomAccess.FlushToDisk), "setLength");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsTransferContentIo(RandomAccess.ReadAsync, RandomAccess.WriteAsync, RandomAccess.SetLength, null!), "flush");
    }

    /// <summary>No partial identity, native facts or path observer may become artifact admission.</summary>
    [TestMethod]
    public void ConstructWhenArtifactQueryIsAbsentRejectsTheMissingObservation()
    {
        WindowsTransferArtifactQueries native = WindowsTransferArtifactQueries.Native;

        TransferArgumentAssertions.Rejects(() => _ = new WindowsTransferArtifactQueries(null!, native.ReadFacts, native.RequirePath), "readIdentity");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsTransferArtifactQueries(native.ReadIdentity, null!, native.RequirePath), "readFacts");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsTransferArtifactQueries(native.ReadIdentity, native.ReadFacts, null!), "requirePath");
    }

    /// <summary>Link deletion cannot be constructed without both disposition and close acknowledgement.</summary>
    [TestMethod]
    public void ConstructWhenRemovalOperationIsAbsentRejectsBeforeAnyNativeCall()
    {
        WindowsTransferArtifactRemoval native = WindowsTransferArtifactRemoval.Native;

        TransferArgumentAssertions.Rejects(() => _ = new WindowsTransferArtifactRemoval(null!, native.Close), "requestDisposition");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsTransferArtifactRemoval(native.RequestDisposition, null!), "close");
        TransferArgumentAssertions.Rejects(() => native.Remove(null!), "handle");
    }

    /// <summary>Each attempt requires its explicit identifier, I/O, observation and deletion boundary.</summary>
    [TestMethod]
    public void ConstructWhenAttemptDependencyIsAbsentRejectsWithoutSupplyingAmbientDefaults()
    {
        WindowsOverwriteDependencies native = WindowsOverwriteDependencies.Native(new DeterministicIdentifierSource());

        TransferArgumentAssertions.Rejects(() => _ = new WindowsOverwriteDependencies(null!, native.Content, native.Artifacts, native.Removal), "identifiers");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsOverwriteDependencies(native.Identifiers, null!, native.Artifacts, native.Removal), "content");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsOverwriteDependencies(native.Identifiers, native.Content, null!, native.Removal), "artifacts");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsOverwriteDependencies(native.Identifiers, native.Content, native.Artifacts, null!), "removal");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsOverwriteCopy(null!, new WindowsDriveBoundary()), "dependencies");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsOverwriteCopy(native, null!), "drives");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsOverwriteRefusalException(null!, null), "reason");
    }

    /// <summary>The existing adapter never silently invents a missing platform owner.</summary>
    [TestMethod]
    public void ConstructWhenLocalAdapterOwnerIsAbsentRejectsBeforePublishingAdapter()
    {
        WindowsDriveBoundary drives = new();
        WindowsRecycleBin recycle = new(new WindowsRecycleShell(), drives);
        WindowsWslFileSystem fileSystem = new(drives);
        WindowsOverwriteDependencies overwrite = WindowsOverwriteDependencies.Native(new DeterministicIdentifierSource());

        TransferArgumentAssertions.Rejects(() => _ = new WindowsLocalAdapterDependencies(null!, fileSystem, drives, overwrite), "recycleBin");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsLocalAdapterDependencies(recycle, null!, drives, overwrite), "fileSystem");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsLocalAdapterDependencies(recycle, fileSystem, null!, overwrite), "drives");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsLocalAdapterDependencies(recycle, fileSystem, drives, null!), "overwrite");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsLocalFileOperationAdapter(new WindowsLocalIoExecutionBoundary(), (WindowsLocalAdapterDependencies)null!), "dependencies");
    }
}
