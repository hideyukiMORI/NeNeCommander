namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents a directory read request rejected because its entry boundary is outside the fixed range.
/// </summary>
public sealed record DirectoryReadRequestRejected : DirectoryReadRequestCreation
{
    internal DirectoryReadRequestRejected()
    {
    }
}
