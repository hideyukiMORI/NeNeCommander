namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents an opaque, bounded provider identity captured during preflight.
/// </summary>
public sealed record FileIdentity
{
    private const int MaximumLength = 512;

    private FileIdentity(string value)
    {
        Value = value;
    }

    /// <summary>Gets the opaque identity value for adapter comparison only.</summary>
    public string Value { get; }

    /// <summary>
    /// Validates an identity token supplied by a provider adapter.
    /// </summary>
    /// <param name="value">Opaque provider identity token.</param>
    /// <returns>An accepted identity or a typed rejection.</returns>
    public static FileIdentityParseOutcome Parse(string? value)
    {
        return value is null || string.IsNullOrWhiteSpace(value) || value.Length > MaximumLength
            ? new FileIdentityRejected()
            : new FileIdentityAccepted(new FileIdentity(value));
    }
}
