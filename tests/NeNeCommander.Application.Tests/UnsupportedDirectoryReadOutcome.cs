using NeNeCommander.Application.Directories;

namespace NeNeCommander.Application.Tests;

internal sealed record UnsupportedDirectoryReadOutcome : DirectoryReadOutcome
{
    private protected override void RestrictDerivation()
    {
    }
}
