namespace NeNeCommander.Application.Launching;

/// <summary>Represents cancellation observed before an external path handoff began.</summary>
public sealed record FileLaunchCancelled : FileLaunchOutcome
{
    internal FileLaunchCancelled()
    {
    }
}
