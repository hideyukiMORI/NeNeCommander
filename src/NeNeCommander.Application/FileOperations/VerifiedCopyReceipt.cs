using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Binds immutable descendant evidence to one frozen source and exact copied target.</summary>
public sealed record VerifiedCopyReceipt
{
    private VerifiedCopyReceipt(FileEntrySnapshot source, VerifiedCopyEntry root, VerifiedTreeBoundary boundary)
    {
        Source = source;
        Root = root;
        Boundary = boundary;
    }

    /// <summary>Gets the source workflow snapshot.</summary>
    public FileEntrySnapshot Source { get; }
    /// <summary>Gets the complete verified tree.</summary>
    public VerifiedCopyEntry Root { get; }
    /// <summary>Gets the original source and target ancestor evidence.</summary>
    public VerifiedTreeBoundary Boundary { get; }

    /// <summary>Creates evidence for a source whose root identity was verified.</summary>
    public static VerifiedCopyReceipt Create(FileEntrySnapshot source, VerifiedCopyEntry root, VerifiedTreeBoundary boundary)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(boundary);
        return !FileSystemPathIdentityComparer.Instance.Equals(source.Path, root.Source.Path) ||
            source.Identity != root.Source.Identity ||
            (source.TransferTarget is FileSystemPath target &&
                !FileSystemPathIdentityComparer.Instance.Equals(target, root.Target.Path))
            ? throw new ArgumentException("The evidence does not match the frozen source and target.", nameof(root))
            : new VerifiedCopyReceipt(source, root, boundary);
    }
}
