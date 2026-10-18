namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents the closed result of constructing a directory read request.
/// </summary>
public abstract record DirectoryReadRequestCreation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected DirectoryReadRequestCreation()
    {
    }
}
