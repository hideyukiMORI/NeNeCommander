using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Derives three visible sibling candidates from one opaque token; it grants no uniqueness or filesystem ownership.</summary>
internal sealed record WindowsTransferArtifactNames
{
    private WindowsTransferArtifactNames(Guid token, WindowsLocalPath recovery, WindowsLocalPath originalBackup, WindowsLocalPath preparedContent)
    {
        Token = token;
        Recovery = recovery;
        OriginalBackup = originalBackup;
        PreparedContent = preparedContent;
    }

    internal Guid Token { get; }
    internal WindowsLocalPath Recovery { get; }
    internal WindowsLocalPath OriginalBackup { get; }
    internal WindowsLocalPath PreparedContent { get; }

    internal static WindowsTransferArtifactNames Create(WindowsLocalPath target, Guid token)
    {
        ArgumentNullException.ThrowIfNull(target);
        WindowsLocalPath parent = target.Parent as WindowsLocalPath ?? throw new ArgumentException("A recovery target cannot be a root.", nameof(target));
        string prefix = Prefix(token);
        WindowsLocalPath recovery = Child(parent, prefix + "-recovery.txt");
        WindowsLocalPath backup = Child(parent, prefix + "-original.bin");
        WindowsLocalPath prepared = Child(parent, prefix + "-prepared.bin");
        return FileSystemPathIdentityComparer.Instance.Equals(target, recovery) ||
            FileSystemPathIdentityComparer.Instance.Equals(target, backup) || FileSystemPathIdentityComparer.Instance.Equals(target, prepared)
            ? throw new ArgumentException("A recovery candidate collides with the original target.", nameof(target))
            : new WindowsTransferArtifactNames(token, recovery, backup, prepared);
    }

    /// <summary>Checks the longest fixed sibling shape before token acquisition, without treating a dummy-token collision as a refusal.</summary>
    internal static void RequireCandidateShape(WindowsLocalPath target)
    {
        ArgumentNullException.ThrowIfNull(target);
        WindowsLocalPath parent = target.Parent as WindowsLocalPath ?? throw new ArgumentException("A recovery target cannot be a root.", nameof(target));
        _ = Child(parent, Prefix(Guid.Empty) + "-prepared.bin");
    }

    private static string Prefix(Guid token)
    {
        return "NeNeCommander-" + token.ToString("N");
    }

    private static WindowsLocalPath Child(WindowsLocalPath parent, string name)
    {
        return parent.Child(name) is PathParseSuccess { Path: WindowsLocalPath child }
            ? child : throw new ArgumentException("The recovery sibling cannot be represented within the canonical path boundary.", nameof(parent));
    }
}
