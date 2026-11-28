namespace NeNeCommander.Application.FileOperations;

/// <summary>Identifies one preparation artifact purpose within the existing transfer operation.</summary>
public abstract record TransferArtifactRole
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the append-only mapping relating the source, target and prepared content objects.</summary>
    public static TransferArtifactRole RecoveryRecord { get; } = new RecoveryRecordCase();

    /// <summary>Gets the original target default-stream backup.</summary>
    public static TransferArtifactRole OriginalBackup { get; } = new OriginalBackupCase();

    /// <summary>Gets the verified source default-stream preparation.</summary>
    public static TransferArtifactRole PreparedContent { get; } = new PreparedContentCase();

    private TransferArtifactRole()
    {
    }

    private sealed record RecoveryRecordCase : TransferArtifactRole
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record OriginalBackupCase : TransferArtifactRole
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record PreparedContentCase : TransferArtifactRole
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
