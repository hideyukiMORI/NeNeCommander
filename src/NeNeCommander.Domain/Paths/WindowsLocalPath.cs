using System;

namespace NeNeCommander.Domain.Paths;

/// <summary>
/// Represents a validated, drive-rooted Windows local filesystem path.
/// </summary>
public sealed record WindowsLocalPath : FileSystemPath
{
    internal WindowsLocalPath(string canonicalText, string drive)
        : base(canonicalText)
    {
        Drive = drive;
    }

    /// <summary>Gets the uppercase drive designator without a separator.</summary>
    public string Drive { get; }

    internal override bool HasSameIdentity(FileSystemPath other)
    {
        return other is WindowsLocalPath local &&
            StringComparer.OrdinalIgnoreCase.Equals(CanonicalText, local.CanonicalText);
    }

    internal override int GetIdentityHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(CanonicalText);
    }
}
