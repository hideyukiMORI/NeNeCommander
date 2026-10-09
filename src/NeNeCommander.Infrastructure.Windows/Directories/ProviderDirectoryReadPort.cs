using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>Routes directory reads once from validated provider identity to its Windows adapter.</summary>
public sealed class ProviderDirectoryReadPort : IDirectoryReadPort
{
    private readonly IDirectoryReadPort _windowsLocal;
    private readonly IDirectoryReadPort _windowsUnc;
    private readonly IDirectoryReadPort _wsl;

    /// <summary>Initializes the provider router over the shared Windows I/O execution boundary.</summary>
    public ProviderDirectoryReadPort(WindowsLocalIoExecutionBoundary executionBoundary)
        : this(
            new WindowsLocalDirectoryReader(executionBoundary),
            new WindowsUncDirectoryReader(executionBoundary),
            new WslDirectoryReader(executionBoundary))
    {
    }

    internal ProviderDirectoryReadPort(
        IDirectoryReadPort windowsLocal,
        IDirectoryReadPort windowsUnc,
        IDirectoryReadPort wsl)
    {
        ArgumentNullException.ThrowIfNull(windowsLocal);
        ArgumentNullException.ThrowIfNull(windowsUnc);
        ArgumentNullException.ThrowIfNull(wsl);
        _windowsLocal = windowsLocal;
        _windowsUnc = windowsUnc;
        _wsl = wsl;
    }

    /// <inheritdoc />
    public Task<DirectoryReadOutcome> ReadAsync(
        DirectoryReadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Location switch
        {
            WindowsLocalPath => _windowsLocal.ReadAsync(request, cancellationToken),
            WindowsUncPath => _windowsUnc.ReadAsync(request, cancellationToken),
            WslPath => _wsl.ReadAsync(request, cancellationToken),
            _ => Task.FromResult(
                DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)),
        };
    }
}
