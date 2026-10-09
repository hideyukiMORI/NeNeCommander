using System;

namespace NeNeCommander.Presentation.WinUI.Locations;

/// <summary>
/// Carries the localized texts of the Locations picker: the overall status (loading or nothing
/// listed) and the state of each section. A text is empty when it has nothing to report.
/// </summary>
public sealed record LocationsPresentationTexts
{
    internal LocationsPresentationTexts(string status, string drives, string wslRoots)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(drives);
        ArgumentNullException.ThrowIfNull(wslRoots);
        Status = status;
        Drives = drives;
        WslRoots = wslRoots;
    }

    /// <summary>Gets the localized loading or nothing-listed text.</summary>
    public string Status { get; }

    /// <summary>Gets the localized drives section failure or unshown-root text.</summary>
    public string Drives { get; }

    /// <summary>Gets the localized WSL section failure text.</summary>
    public string WslRoots { get; }
}
