namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Carries the launcher-owned admission facts of one live run. ADR-0049 moved every identity into
/// the C# owner, so the launcher passes only the configured root and the closed account-home and
/// mount tokens it measured read-only inside the distribution.
/// </summary>
internal sealed record LiveWslRootAdmission
{
    internal const string HomeFactAccepted = "HomeOutsideRoot:v1";
    internal const string MountFactAccepted = "NativeWslFileSystem:v1";

    private LiveWslRootAdmission(string? configuredRoot, string? homeFact, string? mountFact)
    {
        ConfiguredRoot = configuredRoot;
        HomeFact = homeFact;
        MountFact = mountFact;
    }

    internal string? ConfiguredRoot { get; }

    internal string? HomeFact { get; }

    internal string? MountFact { get; }

    internal bool IsComplete => HomeFact is HomeFactAccepted && MountFact is MountFactAccepted;

    internal static LiveWslRootAdmission Create(string? configuredRoot, string? homeFact, string? mountFact)
    {
        return new LiveWslRootAdmission(configuredRoot, homeFact, mountFact);
    }
}
