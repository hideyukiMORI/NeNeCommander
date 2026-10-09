namespace NeNeCommander.Application.Drives;

/// <summary>Represents drive listing cancelled without returning a partial snapshot.</summary>
public sealed record DriveCatalogCancelled : DriveCatalogOutcome
{
    internal DriveCatalogCancelled()
    {
    }
}
