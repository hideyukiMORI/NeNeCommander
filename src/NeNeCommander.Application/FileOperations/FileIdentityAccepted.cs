namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents an accepted provider identity token.
/// </summary>
public sealed record FileIdentityAccepted : FileIdentityParseOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal FileIdentityAccepted(FileIdentity identity)
    {
        Identity = identity;
    }

    /// <summary>Gets the validated identity.</summary>
    public FileIdentity Identity { get; }
}
