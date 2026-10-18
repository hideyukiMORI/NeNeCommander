namespace NeNeCommander.Infrastructure.Windows.Diagnostics;

/// <summary>Represents diagnostic entropy rejected at its trust boundary.</summary>
public sealed record DiagnosticSaltRejected : DiagnosticSaltCreation
{
    private protected override void RestrictDerivation()
    {
    }

    internal DiagnosticSaltRejected()
    {
    }
}
