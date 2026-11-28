using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Checks cancellation before initial creation and after final admission without rewriting the target or discarding retained ownership.</summary>
[TestClass]
public sealed class WindowsTransferPreparationCancellationTests
{
    /// <summary>An already cancelled operation must create no artifact and preserve both original files.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateNativeWhenAlreadyCancelledRetainsNoCreationAttempt()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(fixture.Pair.Target.Path, WindowsTransferRecoveryTestFixture.Token);
        using CancellationTokenSource cancellation = new();
        using WindowsTransferPreparationArtifacts artifacts = WindowsTransferPreparationArtifacts.Create(names, fixture.Pair.Target, WindowsTransferArtifactQueries.Native);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        cancellation.Cancel();

        OperationCanceledException failure = Assert.ThrowsExactly<OperationCanceledException>(() => artifacts.CreateNative(security, cancellation.Token));

        Assert.AreEqual(cancellation.Token, failure.CancellationToken);
        Assert.IsNull(artifacts.Recovery.Receipt);
        Assert.IsNull(artifacts.Backup.Receipt);
        Assert.IsNull(artifacts.Prepared.Receipt);
        Assert.IsFalse(File.Exists(names.Recovery.CanonicalText));
        Assert.IsFalse(File.Exists(names.OriginalBackup.CanonicalText));
        Assert.IsFalse(File.Exists(names.PreparedContent.CanonicalText));
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }

    /// <summary>Cancellation observed at final admission must return cancellation and preserve all three created owners.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateNativeWhenFinalAdmissionCancelsRetainsAllCreatedOwners()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(fixture.Pair.Target.Path, WindowsTransferRecoveryTestFixture.Token);
        using CancellationTokenSource cancellation = new();
        int preparedChecks = 0;
        WindowsTransferArtifactQueries queries = new(
            WindowsTransferArtifactQueries.Native.ReadIdentity, WindowsTransferArtifactQueries.Native.ReadFacts,
            (handle, path) =>
            {
                WindowsTransferArtifactQueries.Native.RequirePath(handle, path);
                if (path == names.PreparedContent && ++preparedChecks == 2)
                {
                    cancellation.Cancel();
                }
            });
        using WindowsTransferPreparationArtifacts artifacts = WindowsTransferPreparationArtifacts.Create(names, fixture.Pair.Target, queries);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();

        OperationCanceledException failure = Assert.ThrowsExactly<OperationCanceledException>(() => artifacts.CreateNative(security, cancellation.Token));

        Assert.AreEqual(cancellation.Token, failure.CancellationToken);
        Assert.AreEqual(2, preparedChecks);
        Assert.IsNotNull(artifacts.Recovery.Receipt);
        Assert.IsNotNull(artifacts.Backup.Receipt);
        Assert.IsNotNull(artifacts.Prepared.Receipt);
        Assert.IsFalse(artifacts.Recovery.Handle.IsClosed);
        Assert.IsFalse(artifacts.Backup.Handle.IsClosed);
        Assert.IsFalse(artifacts.Prepared.Handle.IsClosed);
        Assert.AreEqual(0L, RandomAccess.GetLength(artifacts.Prepared.Handle));
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }

    /// <summary>The owner factory refuses absent queries before parent-path admission or any native creation.</summary>
    [TestMethod]
    [DataRow("same")]
    [DataRow("foreign")]
    public void CreateWhenQueriesAreAbsentPreservesRefusalOrderAndDoesNotCreateArtifacts(string candidate)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsOverwritePreparationFixture foreign = WindowsOverwritePreparationFixture.Create("other source", "other target");
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(
            candidate == "same" ? fixture.Pair.Target.Path : foreign.Pair.Target.Path, WindowsTransferRecoveryTestFixture.Token);

        TransferArgumentAssertions.Rejects(() =>
        {
            using WindowsTransferPreparationArtifacts refused = WindowsTransferPreparationArtifacts.Create(names, fixture.Pair.Target, null!);
        }, "queries");

        Assert.IsFalse(File.Exists(names.Recovery.CanonicalText));
        Assert.IsFalse(File.Exists(names.OriginalBackup.CanonicalText));
        Assert.IsFalse(File.Exists(names.PreparedContent.CanonicalText));
        Assert.IsFalse(fixture.Pair.Target.Handle.IsClosed);
        Assert.IsFalse(foreign.Pair.Target.Handle.IsClosed);
        fixture.Pair.Revalidate(fixture.Pair.Evidence);
        foreign.Pair.Revalidate(foreign.Pair.Evidence);
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
        Assert.AreEqual("other source", foreign.Read(foreign.Pair.Source.Path));
        Assert.AreEqual("other target", foreign.Read(foreign.Pair.Target.Path));
    }

}
