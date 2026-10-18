namespace NeNeCommander.Application.Directories;

/// <summary>Represents a directory read stopped by cancellation without a partial listing.</summary>
public sealed record DirectoryReadCancelled : DirectoryReadOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal DirectoryReadCancelled()
    {
    }
}
