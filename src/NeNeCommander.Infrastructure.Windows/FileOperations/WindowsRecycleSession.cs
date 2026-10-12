using System;
using System.Runtime.InteropServices;
using System.Threading;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns the COM operation and its callback receipt within a single apartment.</summary>
internal sealed class WindowsRecycleSession : IDisposable
{
    // RecycleOnly, AddUndo, EarlyFailure, NoCopyHooks, NoReparse, NoConnected,
    // Silent, NoConfirmation and NoErrorUI. There is no elevation or permanent fallback.
    internal const uint OperationFlags = 0x20000000 | 0x00080000 | 0x00100000 | 0x00800000 |
        0x8000 | 0x2000 | 0x4 | 0x10 | 0x400;
    private readonly IWindowsShellOperation _operation;
    private readonly IWindowsShellEntry _entry;
    private readonly WindowsRecycleProgress _progress;

    internal WindowsRecycleSession(IWindowsShellOperation operation, IWindowsShellEntry entry, WindowsRecycleProgress progress)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(progress);
        _operation = operation;
        _entry = entry;
        _progress = progress;
    }

    internal IWindowsShellEntry? RecycledEntry => _progress.RecycledEntry;
    internal IWindowsShellEntry? ObservedEntry => _progress.ObservedEntry;
    internal int PostResult => _progress.PostResult;
    internal uint PostFlags => _progress.PostFlags;

    internal ProviderStepOutcome Execute(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Delete);
        }
        int result = _operation.SetOperationFlags(OperationFlags);
        if (result != 0)
        {
            return ProviderStepOutcome.Failed(WindowsFileFailureNormalizer.Normalize(result));
        }
        result = _operation.DeleteItem(_entry, _progress);
        return result != 0
            ? ProviderStepOutcome.Failed(WindowsFileFailureNormalizer.Normalize(result))
            : Perform();
    }

    private ProviderStepOutcome Perform()
    {
        try
        {
            int result = _operation.PerformOperations();
            int queried = _operation.GetAnyOperationsAborted(out int aborted);
            return _progress.Complete(result, queried, aborted);
        }
        catch (COMException exception)
        {
            return _progress.Complete(exception.HResult, 0, 0);
        }
        catch (UnauthorizedAccessException exception)
        {
            return _progress.Complete(exception.HResult, 0, 0);
        }
    }

    public void Dispose()
    {
        WindowsRecycleShell.Release(_operation);
        WindowsRecycleShell.Release(_entry);
    }
}
