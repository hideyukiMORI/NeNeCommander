namespace NeNeCommander.Infrastructure.Windows.Paths;

/// <summary>Represents the closed result of a provider-aware operation-root check.</summary>
public abstract record PathContainmentOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected PathContainmentOutcome()
    {
    }
}
