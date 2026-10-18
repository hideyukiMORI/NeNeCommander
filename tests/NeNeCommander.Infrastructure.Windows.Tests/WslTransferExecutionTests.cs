using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves WslFileOperationAdapterTests evidence for WslTransferExecutionTests.</summary>
[TestClass]
public sealed class WslTransferExecutionTests
{
    /// <summary>Proves same- and cross-distribution copy use the gateway's existing copy and verify effects.</summary>
    [TestMethod]
    [DataRow("Ubuntu")]
    [DataRow("Debian")]
    public async Task ExecuteAsyncWhenSameDistributionCopySucceedsReportsCopyAndVerification(string destinationDistribution)
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath source = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source.txt");
        WslPath destination = WslOperationTestContext.Wsl(@"\\wsl.localhost\" + destinationDistribution + @"\target");
        fileSystem.Set(WslOperationTestContext.Entry(source, "source", DirectoryEntryKind.File));
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));
        using FileOperationGateway gateway = new(WslOperationTestContext.Adapter(fileSystem));
        CopyRequest request = Assert.IsInstanceOfType<CopyRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                CopyRequest.Create([source], destination)).Request);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(2, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.AreSame(FileOperationEffectKind.Verified, outcome.Effects[1].Kind);
    }

    /// <summary>Proves same- and cross-distribution move use copy, verify, then source deletion.</summary>
    [TestMethod]
    [DataRow("Ubuntu")]
    [DataRow("Debian")]
    public async Task ExecuteAsyncWhenSameDistributionMoveSucceedsDeletesOnlyAfterVerification(string destinationDistribution)
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath source = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source.txt");
        WslPath destination = WslOperationTestContext.Wsl(@"\\wsl.localhost\" + destinationDistribution + @"\target");
        fileSystem.Set(WslOperationTestContext.Entry(source, "source", DirectoryEntryKind.File));
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));
        using FileOperationGateway gateway = new(WslOperationTestContext.Adapter(fileSystem));
        MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                MoveRequest.Create([source], destination)).Request);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(3, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.AreSame(FileOperationEffectKind.Verified, outcome.Effects[1].Kind);
        Assert.AreSame(FileOperationEffectKind.SourceDeleted, outcome.Effects[2].Kind);
        Assert.HasCount(1, fileSystem.Deleted);
    }

    /// <summary>Proves replacement after preflight is rejected before the first filesystem effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [DataRow("Ubuntu")]
    [DataRow("Debian")]
    public async Task ExecuteAsyncWhenSourceChangesAfterPreflightRejectsWithoutEffect(string destinationDistribution)
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath source = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source.txt");
        WslPath destination = WslOperationTestContext.Wsl(@"\\wsl.localhost\" + destinationDistribution + @"\target");
        fileSystem.Set(WslOperationTestContext.Entry(source, "source", DirectoryEntryKind.File));
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));
        fileSystem.ReplaceSourceWhenTargetChecked = WslOperationTestContext.Entry(source, "replacement", DirectoryEntryKind.File);
        using FileOperationGateway gateway = new(WslOperationTestContext.Adapter(fileSystem));
        CopyRequest request = Assert.IsInstanceOfType<CopyRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                CopyRequest.Create([source], destination)).Request);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.IsEmpty(fileSystem.Copied);
    }

    /// <summary>Proves a failed copy that leaves its target reports the exact partial move effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("Ubuntu")]
    [DataRow("Debian")]
    public async Task ExecuteAsyncWhenCopyFailsAfterTargetCreationReportsPartialEffectAndKeepsSource(string destinationDistribution)
    {
        ScriptedWslFileSystem fileSystem = new() { FailCopyAfterTarget = true };
        WslPath source = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source.txt");
        WslPath destination = WslOperationTestContext.Wsl(@"\\wsl.localhost\" + destinationDistribution + @"\target");
        fileSystem.Set(WslOperationTestContext.Entry(source, "source", DirectoryEntryKind.File));
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));
        using FileOperationGateway gateway = new(WslOperationTestContext.Adapter(fileSystem));
        MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                MoveRequest.Create([source], destination)).Request);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Copy, outcome.Failure);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.CopyTargetCreated, outcome.Effects[0].Kind);
        Assert.HasCount(0, fileSystem.Deleted);
    }

    /// <summary>Proves a copy failure before target creation remains effect-free.</summary>
    [TestMethod]
    [DataRow("Ubuntu")]
    [DataRow("Debian")]
    public async Task ExecuteAsyncWhenCopyFailsBeforeTargetCreationReportsNoEffect(string destinationDistribution)
    {
        ScriptedWslFileSystem fileSystem = new() { FailCopyBeforeTarget = true };
        WslPath source = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source.txt");
        WslPath destination = WslOperationTestContext.Wsl(@"\\wsl.localhost\" + destinationDistribution + @"\target");
        fileSystem.Set(WslOperationTestContext.Entry(source, "source", DirectoryEntryKind.File));
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));
        using FileOperationGateway gateway = new(WslOperationTestContext.Adapter(fileSystem));
        CopyRequest request = Assert.IsInstanceOfType<CopyRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                CopyRequest.Create([source], destination)).Request);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Copy, outcome.Failure);
        Assert.HasCount(0, outcome.Effects);
    }

    /// <summary>Proves failed verification stops a composite move before source deletion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("Ubuntu")]
    [DataRow("Debian")]
    public async Task ExecuteAsyncWhenVerificationFailsKeepsSource(string destinationDistribution)
    {
        ScriptedWslFileSystem fileSystem = new() { MatchResult = false };
        WslPath source = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source.txt");
        WslPath destination = WslOperationTestContext.Wsl(@"\\wsl.localhost\" + destinationDistribution + @"\target");
        fileSystem.Set(WslOperationTestContext.Entry(source, "source", DirectoryEntryKind.File));
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));
        using FileOperationGateway gateway = new(WslOperationTestContext.Adapter(fileSystem));
        MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                MoveRequest.Create([source], destination)).Request);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Verification, outcome.Failure);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.HasCount(0, fileSystem.Deleted);
    }

    /// <summary>Proves source replacement after copy prevents verification and source deletion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [DataRow("Ubuntu")]
    [DataRow("Debian")]
    public async Task ExecuteAsyncWhenSourceChangesAfterCopyStopsBeforeDelete(string destinationDistribution)
    {
        ScriptedWslFileSystem fileSystem = new() { ReplaceSourceAfterCopy = true };
        WslPath source = WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\home\\source.txt");
        WslPath destination = WslOperationTestContext.Wsl(@"\\wsl.localhost\" + destinationDistribution + @"\target");
        fileSystem.Set(WslOperationTestContext.Entry(source, "source", DirectoryEntryKind.File));
        fileSystem.Set(WslOperationTestContext.Entry(destination, "destination", DirectoryEntryKind.Directory));
        using FileOperationGateway gateway = new(WslOperationTestContext.Adapter(fileSystem));
        MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                MoveRequest.Create([source], destination)).Request);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.HasCount(0, fileSystem.Deleted);
    }
}
