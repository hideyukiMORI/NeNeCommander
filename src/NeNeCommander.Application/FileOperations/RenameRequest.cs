using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a validated request to rename one existing entry inside its own parent. The source
/// is the sole entry the gateway inspects; the target is derived by the domain path rules from
/// untrusted name text under the source's parent, so a rename can never leave that parent.
/// </summary>
public sealed record RenameRequest : FileOperationRequest
{
    private RenameRequest(
        ReadOnlyCollection<FileSystemPath> sources,
        FileSystemPath source,
        FileSystemPath target)
        : base(sources)
    {
        Source = source;
        Target = target;
    }

    /// <summary>Gets the frozen existing entry that is renamed.</summary>
    public FileSystemPath Source { get; }

    /// <summary>Gets the frozen path the entry is renamed to, always a direct child of the source's parent.</summary>
    public FileSystemPath Target { get; }

    /// <summary>
    /// Creates a validated immutable rename request. A source without a parent is a provider root
    /// and cannot be renamed. A change that only differs in case is accepted because the canonical
    /// text still changes, so filesystem identity comparison must not decide this.
    /// </summary>
    /// <param name="source">Existing entry to rename.</param>
    /// <param name="name">Untrusted single-segment entry name.</param>
    /// <returns>An accepted request or a typed rejection.</returns>
    public static FileOperationRequestCreation Create(FileSystemPath source, string name)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(name);
        if (source.Parent is not FileSystemPath parent)
        {
            return new FileOperationRequestRejected(FileOperationRequestFailureKind.SourceIsRoot);
        }
        if (parent.Child(name) is not PathParseSuccess child)
        {
            return new FileOperationRequestRejected(FileOperationRequestFailureKind.InvalidName);
        }
        if (child.Path.CanonicalText.Equals(source.CanonicalText, StringComparison.Ordinal))
        {
            return new FileOperationRequestRejected(FileOperationRequestFailureKind.DestinationIsSource);
        }
        List<FileSystemPath> ownedSources = [source];
        return new FileOperationRequestAccepted(
            new RenameRequest(ownedSources.AsReadOnly(), source, child.Path));
    }
}
