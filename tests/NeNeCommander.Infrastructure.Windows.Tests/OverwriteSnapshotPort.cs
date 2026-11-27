using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Obtains the exact gateway-created copy snapshot without granting tests access to application internals or performing effects.</summary>
internal sealed class OverwriteSnapshotPort : IFileOperationPort
{
    private readonly TransferConflict _conflict;
    private FileEntrySnapshot? _captured;

    private OverwriteSnapshotPort(TransferConflict conflict)
    {
        _conflict = conflict;
    }

    internal static async Task<FileEntrySnapshot> CaptureAsync(WindowsOverwritePreparationFixture fixture)
    {
        FileEntrySnapshot source = WindowsOverwriteCopyTests.Snapshot(fixture);
        TransferConflict conflict = TransferConflict.Create(source, fixture.Pair.Target.Path,
            Assert.IsInstanceOfType<PathParseSuccess>(fixture.Root.Path.Child("alternate")).Path, TransferOverwriteCapability.Available(fixture.Pair.Evidence));
        OverwriteSnapshotPort port = new(conflict);
        using FileOperationGateway gateway = new(port);
        CopyRequest request = Assert.IsInstanceOfType<CopyRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            CopyRequest.Create([source.Path], fixture.Root.Path)).Request);
        FileOperationOutcome paused = await gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), CancellationToken.None);
        FileOperationOutcome stopped = await gateway.ResumeAsync(paused.Continuation!,
            TransferConflictResponse.Create(TransferConflictDecision.OverwriteContents, TransferConflictScope.Current),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);
        Assert.AreSame(FileOperationFailureKind.Copy, stopped.Failure);
        return port._captured ?? throw new AssertFailedException("The gateway did not construct the provider copy snapshot.");
    }

    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, FileInspectionPurpose purpose, CancellationToken cancellationToken)
    {
        Assert.AreEqual(_conflict.Source.Path, path);
        return Task.FromResult(FileInspectionOutcome.Succeeded(_conflict.Source));
    }

    public Task<TransferPreflightOutcome> PreflightTransferAsync(IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination, CancellationToken cancellationToken)
    {
        Assert.HasCount(1, sources);
        return Task.FromResult(sources[0].ConflictChoice is null ? TransferPreflightOutcome.Conflicted([_conflict])
            : TransferPreflightOutcome.Succeeded([TransferPlanEntry.Transfer(sources[0], _conflict.ExistingTarget)]));
    }

    public Task<ProviderStepOutcome> CopyAsync(FileEntrySnapshot source, FileSystemPath destination, CancellationToken cancellationToken)
    {
        _captured = source;
        return Task.FromResult(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Copy), ProviderStepReceipt.Empty));
    }

    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(FileEntrySnapshot source, FileSystemPath destination, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Snapshot capture must stop before this provider step.");
    }

    public Task<ProviderStepOutcome> MoveAsync(FileEntrySnapshot source, FileSystemPath destination, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Snapshot capture must stop before this provider step.");
    }

    public Task<CopyVerificationOutcome> VerifyCopyAsync(FileEntrySnapshot source, FileSystemPath destination, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Snapshot capture must stop before this provider step.");
    }

    public Task<ProviderStepOutcome> DeleteAsync(FileEntrySnapshot source, DeletionExecutionMode mode, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Snapshot capture must stop before this provider step.");
    }

    public Task<ProviderStepOutcome> CreateDirectoryAsync(FileEntrySnapshot location, FileSystemPath target, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Snapshot capture must stop before this provider step.");
    }

    public Task<ProviderStepOutcome> CreateEmptyFileAsync(FileEntrySnapshot location, FileSystemPath target, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Snapshot capture must stop before this provider step.");
    }

    public Task<ProviderStepOutcome> RenameAsync(FileEntrySnapshot source, FileSystemPath target, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Snapshot capture must stop before this provider step.");
    }
}
