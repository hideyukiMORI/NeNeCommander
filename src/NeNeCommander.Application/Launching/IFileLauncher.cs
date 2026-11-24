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
    /// Unsupported providers or paths are rejected without a handoff; admission belongs to the adapter.
    /// </summary>
    /// <param name="target">Validated path selected by the user.</param>
    /// <param name="cancellationToken">Token observed before the external handoff starts.</param>
    /// <returns>A closed outcome describing handoff acceptance, cancellation, or failure.</returns>
    public Task<FileLaunchOutcome> LaunchAsync(
        FileSystemPath target,
        CancellationToken cancellationToken);

    /// <summary>
    /// Selects the exact admitted target in its Shell parent. Provider roots are allowed.
    /// Cancellation before the final handoff prevents it; an accepted handoff remains accepted.
    /// This promises neither a visible-window receipt nor stable filesystem identity.
    /// </summary>
    /// <param name="target">Validated item or current folder captured by the application.</param>
    /// <param name="cancellationToken">Observed before parsing and before the final Shell call.</param>
    /// <returns>The existing closed handoff outcome.</returns>
    public Task<FileLaunchOutcome> RevealAsync(
        FileSystemPath target,
        CancellationToken cancellationToken);
}
