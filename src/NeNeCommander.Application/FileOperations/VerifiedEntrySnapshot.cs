using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Binds an entry path to full and stable provider-owned identities.</summary>
public sealed record VerifiedEntrySnapshot
{
    private VerifiedEntrySnapshot(FileSystemPath path, FileIdentity identity, FileObjectIdentity objectIdentity)
    {
        Path = path;
        Identity = identity;
        ObjectIdentity = objectIdentity;
    }

    /// <summary>Gets the exact typed entry path.</summary>
    public FileSystemPath Path { get; }
    /// <summary>Gets the rewrite-sensitive identity.</summary>
    public FileIdentity Identity { get; }
    /// <summary>Gets the identity unaffected by removal of the entry's children.</summary>
    public FileObjectIdentity ObjectIdentity { get; }

    /// <summary>Creates a complete provider observation.</summary>
    public static VerifiedEntrySnapshot Create(FileSystemPath path, FileIdentity identity, FileObjectIdentity objectIdentity)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(objectIdentity);
        return new VerifiedEntrySnapshot(path, identity, objectIdentity);
    }
}
