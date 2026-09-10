using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Launching;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Accepts test-owned file handoffs without invoking the Windows Shell.</summary>
internal sealed class AcceptedFileLauncher : IFileLauncher
{
    public Task<FileLaunchOutcome> LaunchAsync(
        WindowsLocalPath target,
        CancellationToken cancellationToken)
    {
        _ = target;
        _ = cancellationToken;
        return Task.FromResult(FileLaunchOutcome.Accepted());
    }
}
