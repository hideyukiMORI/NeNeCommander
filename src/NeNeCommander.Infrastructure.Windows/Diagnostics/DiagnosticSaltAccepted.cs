namespace NeNeCommander.Infrastructure.Windows.Diagnostics;

/// <summary>Represents accepted diagnostic fingerprint entropy.</summary>
public sealed record DiagnosticSaltAccepted : DiagnosticSaltCreation
{
    private protected override void RestrictDerivation()
    {
    }

    internal DiagnosticSaltAccepted(DiagnosticSalt salt)
    {
        Salt = salt;
    }

    /// <summary>Gets the validated salt whose value remains infrastructure-private.</summary>
    public DiagnosticSalt Salt { get; }
}
