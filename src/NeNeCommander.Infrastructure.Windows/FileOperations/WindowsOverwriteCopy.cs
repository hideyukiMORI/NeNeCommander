using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Completes the existing overwrite attempt inside the caller's local I/O boundary and returns its sole receipt after all awaited work and disposal.</summary>
internal sealed class WindowsOverwriteCopy
{
    private readonly WindowsOverwriteDependencies _dependencies;
    private readonly WindowsDriveBoundary _drives;

    internal WindowsOverwriteCopy(WindowsOverwriteDependencies dependencies, WindowsDriveBoundary drives)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(drives);
        _dependencies = dependencies;
        _drives = drives;
    }

    internal async Task<ProviderStepOutcome> CopyAsync(FileEntrySnapshot source, TransferOverwriteEvidence original, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(original);
        WindowsOverwriteAttempt? attempt = null;
        ProviderStepCompletion completion;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            WindowsLocalPath target = original.Target.Observation.Entry.Path as WindowsLocalPath ??
                throw new IOException("Overwrite requires the exact original local target.");
            using WindowsOverwritePairLease pair = WindowsOverwritePairLease.Open(source, target, _drives);
            pair.Revalidate(original);
            using (attempt = WindowsOverwriteAttempt.Create(pair, original, _dependencies))
            {
                _ = await attempt.PrepareAsync(cancellationToken).ConfigureAwait(false);
                await attempt.OverwritePreparedAsync(cancellationToken).ConfigureAwait(false);
                await attempt.AppendCompletionAsync(cancellationToken).ConfigureAwait(false);
                await attempt.CleanupAsync(cancellationToken).ConfigureAwait(false);
            }
            completion = ProviderStepCompletion.Succeeded;
        }
        catch (OperationCanceledException)
        {
            completion = ProviderStepCompletion.Cancelled;
        }
        catch (UnauthorizedAccessException exception)
        {
            completion = ProviderStepCompletion.Failed(WindowsLocalFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Copy));
        }
        catch (IOException exception)
        {
            completion = ProviderStepCompletion.Failed(WindowsLocalFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Copy));
        }
        ProviderStepReceipt receipt = attempt?.Receipt ?? ProviderStepReceipt.Create([], ProviderTargetObservation.Untouched(
            ObservedTransferEntry.Create(original.Target.Observation.Entry, TransferPathObservation.LastKnownPath)), []);
        return ProviderStepOutcome.Create(completion, receipt);
    }
}
