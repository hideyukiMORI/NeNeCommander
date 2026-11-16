using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>An ordinary, non-reparse ancestor directory with its observed file identifier.</summary>
internal sealed record PresentDirectory : DirectoryBaseline
{
    internal PresentDirectory(WindowsLocalPath path, string identifier)
        : base(path)
    {
        Identifier = identifier;
    }

    internal string Identifier { get; }
}
