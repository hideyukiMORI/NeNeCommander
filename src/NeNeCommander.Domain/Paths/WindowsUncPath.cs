using System;

namespace NeNeCommander.Domain.Paths;

/// <summary>
/// Represents a validated Windows UNC path that is not a WSL namespace path.
/// </summary>
public sealed record WindowsUncPath : FileSystemPath
{
    internal WindowsUncPath(string canonicalText, string server, string share)
        : base(canonicalText)
    {
        Server = server;
        Share = share;
    }

    /// <summary>Gets the server component with its original casing.</summary>
    public string Server { get; }

    /// <summary>Gets the share component with its original casing.</summary>
    public string Share { get; }

    /// <inheritdoc />
    public override FileSystemPath? Parent =>
        CanonicalText.Length <= RootLength
            ? null
            : new WindowsUncPath(RemoveLastSegment(CanonicalText, RootLength), Server, Share);

    private int RootLength => 2 + Server.Length + 1 + Share.Length + 1;

    internal override bool HasSameIdentity(FileSystemPath other)
    {
        return other is WindowsUncPath unc &&
            StringComparer.OrdinalIgnoreCase.Equals(CanonicalText, unc.CanonicalText);
    }

    internal override int GetIdentityHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(CanonicalText);
    }
}
