using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>An ancestor directory that did not exist when observed.</summary>
internal sealed record AbsentDirectory : DirectoryBaseline
{
    internal AbsentDirectory(WindowsLocalPath path)
        : base(path)
    {
    }
}
