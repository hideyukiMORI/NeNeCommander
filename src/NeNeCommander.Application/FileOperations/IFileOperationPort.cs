using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Defines the sole provider-neutral side-effect boundary used by <see cref="FileOperationGateway"/>.
/// </summary>
public interface IFileOperationPort
{
    /// <summary>Captures identity and capabilities before any mutation begins.</summary>
    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken);

    /// <summary>Validates destination containment, recursion, capability, and every source collision before a copy or move.</summary>
    public Task<ProviderStepOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken);

    /// <summary>Copies one frozen source entry beneath a validated destination.</summary>
    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken);

    /// <summary>Verifies a copied entry against the frozen source identity and content contract.</summary>
    public Task<ProviderStepOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken);

    /// <summary>Deletes one frozen entry using the explicitly selected execution mode.</summary>
    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken);

    /// <summary>Creates one directory at the target directly beneath the frozen, revalidated location.</summary>
    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken);

    /// <summary>Renames one frozen, revalidated entry to a target that must share the source's parent.</summary>
    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source,
        FileSystemPath target,
        CancellationToken cancellationToken);
}
