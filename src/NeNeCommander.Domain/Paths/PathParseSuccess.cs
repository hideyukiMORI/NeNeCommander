namespace NeNeCommander.Domain.Paths;

/// <summary>
/// Represents a successfully parsed and canonicalized filesystem path.
/// </summary>
public sealed record PathParseSuccess : PathParseOutcome
{
    internal PathParseSuccess(FileSystemPath path)
    {
        Path = path;
    }

    /// <summary>Gets the validated path.</summary>
    public FileSystemPath Path { get; }
}
