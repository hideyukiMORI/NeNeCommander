namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a rejected provider identity token.
/// </summary>
public sealed record FileIdentityRejected : FileIdentityParseOutcome
{
    internal FileIdentityRejected()
    {
    }
}
