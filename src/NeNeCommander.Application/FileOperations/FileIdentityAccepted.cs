namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents an accepted provider identity token.
/// </summary>
public sealed record FileIdentityAccepted : FileIdentityParseOutcome
{
    internal FileIdentityAccepted(FileIdentity identity)
    {
        Identity = identity;
    }

    /// <summary>Gets the validated identity.</summary>
    public FileIdentity Identity { get; }
}
