using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Application.Settings;

/// <summary>
/// Defines the sole boundary for reading persisted user settings. Implementations are queries:
/// they never create, repair, or rewrite the stored document as a side effect of a read.
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
}
