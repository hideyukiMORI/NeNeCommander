using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Tests;

internal sealed class UnusedFileOperationPort : IFileOperationPort
{
    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Presentation tests never reach the file-operation port.");
    }

    public Task<ProviderStepOutcome> PreflightMoveAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Presentation tests never reach the file-operation port.");
    }

    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Presentation tests never reach the file-operation port.");
    }

    public Task<ProviderStepOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Presentation tests never reach the file-operation port.");
    }

    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Presentation tests never reach the file-operation port.");
    }
}
