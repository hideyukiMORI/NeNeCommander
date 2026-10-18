using NeNeCommander.Application.Wsl;

namespace NeNeCommander.Application.Locations;

/// <summary>
/// Represents the WSL section of an open Locations picker: either listed or failed with its reason.
/// It is decided independently of the drives section, so one failure never hides the other.
/// </summary>
public abstract record WslRootSection
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected WslRootSection()
    {
    }

    internal static WslRootSection Of(WslDistributionCatalogOutcome outcome)
    {
        return outcome is WslDistributionCatalogSucceeded succeeded
            ? new WslRootSectionListed(succeeded)
            : new WslRootSectionFailed(((WslDistributionCatalogFailed)outcome).Failure);
    }
}
