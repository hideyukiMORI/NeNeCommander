namespace NeNeCommander.Application.FileOperations;

/// <summary>Specifies which observations the gateway needs before the requested operation.</summary>
public abstract record FileInspectionPurpose
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets identity capture without an ordinary-deletion capability query.</summary>
    public static FileInspectionPurpose IdentityOnly { get; } = new IdentityOnlyPurpose();

    /// <summary>Gets identity capture with the existing ordinary-deletion capability query.</summary>
    public static FileInspectionPurpose Deletion { get; } = new DeletionPurpose();

    private FileInspectionPurpose()
    {
    }

    private sealed record IdentityOnlyPurpose : FileInspectionPurpose
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record DeletionPurpose : FileInspectionPurpose
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
