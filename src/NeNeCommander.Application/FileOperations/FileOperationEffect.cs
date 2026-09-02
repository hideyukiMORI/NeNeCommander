using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Records one exact side effect completed before an operation outcome was returned.
/// </summary>
public sealed record FileOperationEffect
{
    private FileOperationEffect(FileSystemPath source, FileOperationEffectKind kind)
    {
        Source = source;
        Kind = kind;
    }

    /// <summary>Gets the source identity associated with the completed effect.</summary>
    public FileSystemPath Source { get; }

    /// <summary>Gets the completed effect kind.</summary>
    public FileOperationEffectKind Kind { get; }

    internal static FileOperationEffect Create(FileSystemPath source, FileOperationEffectKind kind)
    {
        return new FileOperationEffect(source, kind);
    }
}
