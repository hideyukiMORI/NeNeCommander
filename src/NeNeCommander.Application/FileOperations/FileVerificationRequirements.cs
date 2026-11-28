namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains the required file observations without granting transfer or deletion permission.</summary>
public abstract record FileVerificationRequirements
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Requires the existing default-stream bytes and provider identity contract.</summary>
    public static FileVerificationRequirements DefaultStream { get; } = new DefaultStreamRequirements();

    /// <summary>Additionally requires a single-link regular local NTFS file, only its unnamed stream, and Normal or Archive attributes.</summary>
    public static FileVerificationRequirements OverwriteContents { get; } = new OverwriteContentsRequirements();

    private FileVerificationRequirements()
    {
    }

    private sealed record DefaultStreamRequirements : FileVerificationRequirements
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record OverwriteContentsRequirements : FileVerificationRequirements
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
