using System;
using System.Runtime.InteropServices;
using System.Threading;
using NeNeCommander.Application.Launching;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Launching;

/// <summary>Owns parsing, one selection handoff and unconditional release of its absolute PIDL.</summary>
internal sealed partial class WindowsShellItemSelection
{
    private readonly Func<string, WindowsShellItemParseResult> _parse;
    private readonly Func<IntPtr, int> _select;
    private readonly Action<IntPtr> _free;

    internal WindowsShellItemSelection()
        : this(Parse, Select, Marshal.FreeCoTaskMem)
    {
    }

    internal WindowsShellItemSelection(
        Func<string, WindowsShellItemParseResult> parse,
        Func<IntPtr, int> select,
        Action<IntPtr> free)
    {
        ArgumentNullException.ThrowIfNull(parse);
        ArgumentNullException.ThrowIfNull(select);
        ArgumentNullException.ThrowIfNull(free);
        _parse = parse;
        _select = select;
        _free = free;
    }

    internal FileLaunchOutcome Reveal(FileSystemPath target, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (cancellationToken.IsCancellationRequested)
        {
            return FileLaunchOutcome.Cancelled();
        }
        WindowsShellItemParseResult parsed = _parse(target.CanonicalText);
        try
        {
            return SelectParsed(parsed, cancellationToken);
        }
        finally
        {
            if (parsed.ItemIdList != IntPtr.Zero)
            {
                _free(parsed.ItemIdList);
            }
        }
    }

    private FileLaunchOutcome SelectParsed(WindowsShellItemParseResult parsed, CancellationToken cancellationToken)
    {
        if (parsed.HResult < 0)
        {
            return Failed(parsed.HResult);
        }
        if (parsed.ItemIdList == IntPtr.Zero)
        {
            return FileLaunchOutcome.Failed(FileLaunchFailureKind.ShellRejected);
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return FileLaunchOutcome.Cancelled();
        }
        int result = _select(parsed.ItemIdList);
        return result >= 0 ? FileLaunchOutcome.Accepted() : Failed(result);
    }

    private static FileLaunchOutcome Failed(int result)
    {
        const int Win32FailurePrefix = unchecked((int)0x80070000);
        const int HighWordMask = unchecked((int)0xFFFF0000);
        return FileLaunchOutcome.Failed((result & HighWordMask) == Win32FailurePrefix
            ? WindowsShellFileLauncher.NormalizeFailure(result & 0xFFFF)
            : FileLaunchFailureKind.ShellRejected);
    }

    internal static WindowsShellItemParseResult Parse(string path)
    {
        int result = SHParseDisplayName(path, IntPtr.Zero, out IntPtr itemIdList, 0, out _);
        return new WindowsShellItemParseResult(result, itemIdList);
    }

    private static int Select(IntPtr itemIdList)
    {
        return SHOpenFolderAndSelectItems(itemIdList, 0, IntPtr.Zero, 0);
    }

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHParseDisplayName(
        string name,
        IntPtr bindContext,
        out IntPtr itemIdList,
        uint requestedAttributes,
        out uint returnedAttributes);

    [LibraryImport("shell32.dll")]
    private static partial int SHOpenFolderAndSelectItems(
        IntPtr itemIdList,
        uint childCount,
        IntPtr children,
        uint flags);
}
