using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>One observed ancestor directory of the settings document.</summary>
internal abstract record DirectoryBaseline
{
    protected DirectoryBaseline(WindowsLocalPath path)
    {
        Path = path;
    }

    internal WindowsLocalPath Path { get; }
}
