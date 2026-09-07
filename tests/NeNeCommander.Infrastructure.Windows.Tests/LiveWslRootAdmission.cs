using System;
using System.Linq;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed record LiveWslRootAdmission
{
    internal const string HomeFactAccepted = "HomeOutsideRoot:v1";
    internal const string MountFactAccepted = "NativeWslFileSystem:v1";

    private LiveWslRootAdmission(
        string? configuredRoot,
        string? temporaryRootIdentity,
        string? configuredRootIdentity,
        string? homeFact,
        string? mountFact)
    {
        ConfiguredRoot = configuredRoot;
        TemporaryRootIdentity = temporaryRootIdentity;
        ConfiguredRootIdentity = configuredRootIdentity;
        HomeFact = homeFact;
        MountFact = mountFact;
    }

    internal string? ConfiguredRoot { get; }

    internal string? TemporaryRootIdentity { get; }

    internal string? ConfiguredRootIdentity { get; }

    internal string? HomeFact { get; }

    internal string? MountFact { get; }

    internal bool IsComplete =>
        IsIdentifier(TemporaryRootIdentity) &&
        IsIdentifier(ConfiguredRootIdentity) &&
        HomeFact is HomeFactAccepted &&
        MountFact is MountFactAccepted;

    internal static LiveWslRootAdmission Create(
        string? configuredRoot,
        string? temporaryRootIdentity,
        string? configuredRootIdentity,
        string? homeFact,
        string? mountFact)
    {
        return new LiveWslRootAdmission(
            configuredRoot,
            temporaryRootIdentity,
            configuredRootIdentity,
            homeFact,
            mountFact);
    }

    private static bool IsIdentifier(string? value)
    {
        return value is { Length: 48 } && value.All(Uri.IsHexDigit);
    }
}
