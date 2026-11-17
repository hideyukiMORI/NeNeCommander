using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Records which provider router members reach one adapter slot, in call order, and answers each
/// with a fixed outcome so the router's selection is the only observed behavior.
/// </summary>
internal sealed class RecordingFileOperationPort : IFileOperationPort
{
    internal int InspectionCount { get; private set; }

    internal List<string> Calls { get; } = [];

    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
    {
        InspectionCount++;
        return Task.FromResult(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
    }

    public Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        Calls.Add("preflight");
        return Task.FromResult(TransferPreflightOutcome.Succeeded([]));
    }

    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        Calls.Add("capability");
        return Task.FromResult(AtomicMoveCapabilityOutcome.Unsupported);
    }

    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Step("move");
    }

    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Step("copy");
    }

    public Task<CopyVerificationOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        Calls.Add("verify");
        return Task.FromResult(VerificationTestEvidence.FromStep(ProviderStepOutcome.Succeeded(), source, destination));
    }

    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        return Step("delete");
    }

    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        return Step("create");
    }

    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        return Step("rename");
    }

    private Task<ProviderStepOutcome> Step(string name)
    {
        Calls.Add(name);
        return Task.FromResult(ProviderStepOutcome.Succeeded());
    }
}
