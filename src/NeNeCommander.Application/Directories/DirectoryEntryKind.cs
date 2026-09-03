namespace NeNeCommander.Application.Directories;

/// <summary>
/// Identifies the closed kind of one direct directory entry as reported by its provider.
/// </summary>
public abstract record DirectoryEntryKind
{
    /// <summary>Gets the kind for an entry that can itself be read as a directory.</summary>
    public static DirectoryEntryKind Directory { get; } = new DirectoryKind();

    /// <summary>Gets the kind for an entry that cannot be read as a directory.</summary>
    public static DirectoryEntryKind File { get; } = new FileKind();

    private DirectoryEntryKind()
    {
    }

    private sealed record DirectoryKind : DirectoryEntryKind;
    private sealed record FileKind : DirectoryEntryKind;
}
