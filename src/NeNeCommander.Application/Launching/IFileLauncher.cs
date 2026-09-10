using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Launching;

/// <summary>
/// Defines the sole provider boundary that hands one validated filesystem path to its associated
/// application without exposing shell verbs, arguments, or working-directory policy to callers.
/// </summary>
public interface IFileLauncher
{
    /// <summary>
    /// Attempts one user-requested path handoff. Cancellation can prevent a handoff only when it
    /// is observed before the provider starts it; accepted work does not own the launched process.
    /// </summary>
    /// <param name="target">Validated path selected by the user.</param>
    /// <param name="cancellationToken">Token observed before the external handoff starts.</param>
    /// <returns>A closed outcome describing handoff acceptance, cancellation, or failure.</returns>
    public Task<FileLaunchOutcome> LaunchAsync(
        WindowsLocalPath target,
        CancellationToken cancellationToken);
}
