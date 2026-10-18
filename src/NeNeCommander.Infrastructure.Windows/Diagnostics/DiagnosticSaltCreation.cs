namespace NeNeCommander.Infrastructure.Windows.Diagnostics;

/// <summary>Represents the closed result of validating diagnostic fingerprint entropy.</summary>
public abstract record DiagnosticSaltCreation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected DiagnosticSaltCreation()
    {
    }
}
