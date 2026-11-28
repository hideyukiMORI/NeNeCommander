using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves record eligibility is rechecked before consuming saved recovery bytes or beginning an append.</summary>
[TestClass]
public sealed class WindowsTransferRecoveryReadAdmissionTests
{
    /// <summary>A newly hidden owned record refuses before content I/O while retaining the original frame observations.</summary>
    [TestMethod]
    [DataRow("initial")]
    [DataRow("append")]
    [DataRow("completed")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ReadmissionAsyncWhenRecordShapeChangesRefusesBeforeContentIoAsync(string operation)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(files.Pair.Source.Path.CanonicalText, files.Pair.Target.Path.CanonicalText);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord,
            mapping.Names.Recovery, files.Pair.Target, WindowsTransferArtifactQueries.Native);
        owner.CreateNative(security);
        _ = await WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, WindowsTransferContentIo.Native, CancellationToken.None);
        if (operation == "completed")
        {
            _ = await WindowsTransferRecoveryWriter.AppendCompletionAsync(owner, mapping, WindowsTransferContentIo.Native, CancellationToken.None);
        }
        ArtifactContentObservation saved = Known(owner).Content;
        string content = files.Read(mapping.Names.Recovery);
        int reads = 0;
        int writes = 0;
        int flushes = 0;
        WindowsTransferContentIo io = new((handle, buffer, offset, token) => { reads++; return RandomAccess.ReadAsync(handle, buffer, offset, token); },
            (handle, buffer, offset, token) => { writes++; return RandomAccess.WriteAsync(handle, buffer, offset, token); },
            (handle, length) => { writes++; RandomAccess.SetLength(handle, length); }, handle => { flushes++; RandomAccess.FlushToDisk(handle); });
        FileAttributes attributes = File.GetAttributes(mapping.Names.Recovery.CanonicalText);
        File.SetAttributes(mapping.Names.Recovery.CanonicalText, attributes | FileAttributes.Hidden);
        try
        {
            _ = await Assert.ThrowsAsync<IOException>(() => InvokeAsync(owner, mapping, io, operation));
            Assert.AreEqual(0, reads);
            Assert.AreEqual(0, writes);
            Assert.AreEqual(0, flushes);
            Assert.AreEqual(saved, Known(owner).Content);
            Assert.AreEqual(ArtifactLinkObservation.Retained, Known(owner).Link);
            Assert.AreEqual(content, files.Read(mapping.Names.Recovery));
        }
        finally
        {
            File.SetAttributes(mapping.Names.Recovery.CanonicalText, attributes);
        }
    }

    private static Task InvokeAsync(WindowsTransferArtifactOwner owner, WindowsTransferRecoveryMapping mapping, WindowsTransferContentIo io, string operation)
    {
        return operation switch
        {
            "initial" => WindowsTransferRecoveryWriter.RequireInitialAsync(owner, mapping, io, CancellationToken.None),
            "append" => WindowsTransferRecoveryWriter.AppendCompletionAsync(owner, mapping, io, CancellationToken.None),
            "completed" => WindowsTransferRecoveryWriter.RequireCompletedAsync(owner, mapping, io, CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
    }

    private static IdentifiedTransferArtifact Known(WindowsTransferArtifactOwner owner)
    {
        return Assert.IsInstanceOfType<IdentifiedTransferArtifact>(owner.Receipt);
    }
}
