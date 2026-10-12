using System;
using System.Threading;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Vetoes unrequested effects and records the exact completed recycle receipt.</summary>
internal sealed class WindowsRecycleProgress : IWindowsRecycleSink
{
    internal const int Abort = unchecked((int)0x80004004);
    internal const uint RecycleFlag = 0x80;
    private const int CompletedWithoutChildTraversal = 0x00270008;
    private readonly FileEntrySnapshot _source;
    private readonly CancellationToken _cancellationToken;
    private int _preCount;
    private int _postCount;
    private bool _admitted;

    internal WindowsRecycleProgress(FileEntrySnapshot source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _cancellationToken = cancellationToken;
    }

    internal IWindowsShellEntry? RecycledEntry { get; private set; }
    internal FileOperationFailureKind? Failure { get; private set; }
    internal IWindowsShellEntry? ObservedEntry { get; private set; }
    internal int PostResult { get; private set; }
    internal uint PostFlags { get; private set; }

    public int PreDeleteItem(uint flags, IWindowsShellEntry entry)
    {
        _preCount++;
        if (_preCount != 1 || Failure is not null || (flags & RecycleFlag) == 0 || !MatchesSource(entry))
        {
            return Reject(FileOperationFailureKind.ProviderUnavailable);
        }
        if (_cancellationToken.IsCancellationRequested)
        {
            return Reject(FileOperationFailureKind.Delete);
        }
        FileOperationFailureKind? refusal = WindowsRecycleAdmission.Revalidate(_source);
        if (refusal is not null)
        {
            return Reject(refusal);
        }
        _admitted = true;
        return 0;
    }

    public int PostDeleteItem(uint flags, IWindowsShellEntry entry, int result, IWindowsShellEntry? created)
    {
        _postCount++;
        PostResult = result;
        PostFlags = flags;
        if (_postCount == 1)
        {
            ObservedEntry = created;
        }
        if (_preCount != 1 || _postCount != 1 || !_admitted ||
            (flags & RecycleFlag) == 0 || !MatchesSource(entry) ||
            result is not 0 and not CompletedWithoutChildTraversal || created is null)
        {
            return Reject(result < 0 ? WindowsFileFailureNormalizer.Normalize(result) : FileOperationFailureKind.Delete);
        }
        RecycledEntry = created;
        return 0;
    }

    internal ProviderStepOutcome Complete(int performResult, int abortQueryResult, int aborted)
    {
        FileOperationFailureKind? failure = Failure ??
            (performResult != 0 ? WindowsFileFailureNormalizer.Normalize(performResult) :
                abortQueryResult != 0 ? WindowsFileFailureNormalizer.Normalize(abortQueryResult) :
                aborted != 0 || RecycledEntry is null ? FileOperationFailureKind.Delete : null);
        return failure is null
            ? ProviderStepOutcome.Succeeded()
            : RecycledEntry is null
                ? ProviderStepOutcome.Failed(failure)
                : ProviderStepOutcome.FailedAfterEffect(failure, ProviderStepEffectKind.Recycled);
    }

    private bool MatchesSource(IWindowsShellEntry entry)
    {
        int result = entry.GetDisplayName(0x80058000, out string text);
        return result == 0 && FileSystemPath.Parse(text) is PathParseSuccess parsed &&
            FileSystemPathIdentityComparer.Instance.Equals(_source.Path, parsed.Path);
    }

    private int Reject(FileOperationFailureKind failure)
    {
        Failure ??= failure;
        return Abort;
    }

    public int StartOperations()
    {
        return 0;
    }
    public int FinishOperations(int result)
    {
        return result < 0 ? Reject(WindowsFileFailureNormalizer.Normalize(result)) : 0;
    }
    public int PreRenameItem(uint flags, IntPtr entry, string name)
    {
        return Reject(FileOperationFailureKind.ProviderUnavailable);
    }
    public int PostRenameItem(uint flags, IntPtr entry, string name, int result, IntPtr created)
    {
        return Reject(FileOperationFailureKind.ProviderUnavailable);
    }
    public int PreMoveItem(uint flags, IntPtr entry, IntPtr destination, string name)
    {
        return Reject(FileOperationFailureKind.ProviderUnavailable);
    }
    public int PostMoveItem(uint flags, IntPtr entry, IntPtr destination, string name, int result, IntPtr created)
    {
        return Reject(FileOperationFailureKind.ProviderUnavailable);
    }
    public int PreCopyItem(uint flags, IntPtr entry, IntPtr destination, string name)
    {
        return Reject(FileOperationFailureKind.ProviderUnavailable);
    }
    public int PostCopyItem(uint flags, IntPtr entry, IntPtr destination, string name, int result, IntPtr created)
    {
        return Reject(FileOperationFailureKind.ProviderUnavailable);
    }
    public int PreNewItem(uint flags, IntPtr destination, string name)
    {
        return Reject(FileOperationFailureKind.ProviderUnavailable);
    }
    public int PostNewItem(uint flags, IntPtr destination, string name, string template, uint attributes, int result, IntPtr created)
    {
        return Reject(FileOperationFailureKind.ProviderUnavailable);
    }
    public int UpdateProgress(uint total, uint completed)
    {
        return _cancellationToken.IsCancellationRequested ? Reject(FileOperationFailureKind.Delete) : 0;
    }
    public int ResetTimer()
    {
        return 0;
    }
    public int PauseTimer()
    {
        return 0;
    }
    public int ResumeTimer()
    {
        return 0;
    }

}
