namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents the closed result of constructing a file operation request.
/// </summary>
public abstract record FileOperationRequestCreation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected FileOperationRequestCreation()
    {
    }
}
