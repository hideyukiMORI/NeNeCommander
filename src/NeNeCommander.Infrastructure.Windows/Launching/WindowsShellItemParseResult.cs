using System;

namespace NeNeCommander.Infrastructure.Windows.Launching;

/// <summary>Carries one native parse result and its caller-owned absolute item identifier list.</summary>
internal sealed record WindowsShellItemParseResult
{
    internal WindowsShellItemParseResult(int result, IntPtr itemIdList)
    {
        HResult = result;
        ItemIdList = itemIdList;
    }

    internal int HResult { get; }

    internal IntPtr ItemIdList { get; }
}
