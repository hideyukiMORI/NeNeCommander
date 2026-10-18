using NeNeCommander.Application.Drives;

namespace NeNeCommander.Application.Locations;

/// <summary>
/// Represents the drives section of an open Locations picker: either listed or failed with its
/// reason. It is decided independently of the WSL section, so one failure never hides the other.
/// </summary>
public abstract record DriveSection
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected DriveSection()
    {
    }

    internal static DriveSection Of(DriveCatalogOutcome outcome)
    {
        return outcome is DriveCatalogSucceeded succeeded
            ? new DriveSectionListed(succeeded)
            : new DriveSectionFailed(((DriveCatalogFailed)outcome).Failure);
    }
}
