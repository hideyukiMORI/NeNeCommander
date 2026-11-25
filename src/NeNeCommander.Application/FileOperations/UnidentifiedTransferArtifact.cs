using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Preserves a possible preparation at an attempted name without asserting object identity or ownership.</summary>
public sealed record UnidentifiedTransferArtifact : TransferArtifactReceipt
{
    internal UnidentifiedTransferArtifact(TransferArtifactRole role, FileSystemPath candidatePath)
        : base(role)
    {
        CandidatePath = candidatePath;
    }

    /// <summary>Gets only the attempted path, not a verified current or previously confirmed object association.</summary>
    public FileSystemPath CandidatePath { get; }

    private protected override void RestrictDerivation()
    {
    }
}
