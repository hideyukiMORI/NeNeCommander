namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Identifies one closed side effect completed during a file operation.
/// </summary>
public abstract record FileOperationEffectKind
{
    /// <summary>Gets the effect indicating destination bytes were copied.</summary>
    public static FileOperationEffectKind Copied { get; } = new CopiedEffect();

    /// <summary>Gets the effect indicating the destination copy was verified.</summary>
    public static FileOperationEffectKind Verified { get; } = new VerifiedEffect();

    /// <summary>Gets the effect indicating the source was deleted after a move.</summary>
    public static FileOperationEffectKind SourceDeleted { get; } = new SourceDeletedEffect();

    /// <summary>Gets the effect indicating an item was sent to provider recycle.</summary>
    public static FileOperationEffectKind Recycled { get; } = new RecycledEffect();

    /// <summary>Gets the effect indicating an item was permanently deleted.</summary>
    public static FileOperationEffectKind PermanentlyDeleted { get; } = new PermanentlyDeletedEffect();

    /// <summary>Gets the effect indicating a directory was created at the effect's path.</summary>
    public static FileOperationEffectKind DirectoryCreated { get; } = new DirectoryCreatedEffect();

    private FileOperationEffectKind()
    {
    }

    private sealed record CopiedEffect : FileOperationEffectKind;
    private sealed record VerifiedEffect : FileOperationEffectKind;
    private sealed record SourceDeletedEffect : FileOperationEffectKind;
    private sealed record RecycledEffect : FileOperationEffectKind;
    private sealed record PermanentlyDeletedEffect : FileOperationEffectKind;
    private sealed record DirectoryCreatedEffect : FileOperationEffectKind;
}
