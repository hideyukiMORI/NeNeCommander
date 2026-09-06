using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Application.Settings;

/// <summary>
/// Defines the sole boundary for reading and atomically writing persisted user settings. Reads
/// remain pure queries; writes accept only complete settings and never repair a rejected document.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Reads the stored settings document once, bounding its size before parsing it.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token observed by the underlying read; cancellation surfaces as
    /// <see cref="System.OperationCanceledException"/> from the awaited call rather than as settings.
    /// </param>
    /// <returns>Complete settings, the absent outcome, or a typed rejection.</returns>
    public Task<SettingsReadOutcome> ReadAsync(CancellationToken cancellationToken);

    /// <summary>Writes one complete settings document through the declared settings boundary.</summary>
    /// <param name="settings">Complete settings to serialize.</param>
    /// <param name="cancellationToken">Token observed only before the first mutation.</param>
    /// <returns>The closed write outcome, including any temporary-artifact effect.</returns>
    public Task<SettingsWriteOutcome> WriteAsync(
        UserSettings settings,
        CancellationToken cancellationToken);
}
