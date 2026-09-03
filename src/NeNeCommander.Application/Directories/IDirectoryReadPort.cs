using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Defines the sole provider-neutral query boundary for reading the direct entries of one location.
/// </summary>
public interface IDirectoryReadPort
{
    /// <summary>
    /// Reads the direct entries of the requested location without mutating any state.
    /// </summary>
    /// <param name="request">Validated read request whose entry boundary the adapter must honor.</param>
    /// <param name="cancellationToken">Token observed before each entry; cancellation yields no partial listing.</param>
    /// <returns>A listing, the cancelled outcome, or a normalized expected failure.</returns>
    public Task<DirectoryReadOutcome> ReadAsync(DirectoryReadRequest request, CancellationToken cancellationToken);
}
