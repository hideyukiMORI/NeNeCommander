namespace NeNeCommander.Application.Launching;

/// <summary>
/// Represents a path handoff accepted by the external provider. It does not assert that a new
/// process was created or that an application finished opening the file.
/// </summary>
public sealed record FileLaunchAccepted : FileLaunchOutcome
{
    internal FileLaunchAccepted()
    {
    }
}
