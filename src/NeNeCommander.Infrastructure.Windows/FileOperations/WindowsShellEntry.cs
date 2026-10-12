using System;
using System.Runtime.InteropServices.Marshalling;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns one unique native entry wrapper without exposing its COM interface to policy tests.</summary>
internal sealed class WindowsShellEntry : IWindowsShellEntry, IDisposable
{
    internal WindowsShellEntry(INativeWindowsShellEntry native)
    {
        ArgumentNullException.ThrowIfNull(native);
        Native = native;
    }

    internal INativeWindowsShellEntry Native { get; }

    public int GetDisplayName(uint form, out string name)
    {
        return Native.GetDisplayName(form, out name);
    }

    public int GetAttributes(uint mask, out uint attributes)
    {
        return Native.GetAttributes(mask, out attributes);
    }

    public void Dispose()
    {
        object native = Native;
        ((ComObject)native).FinalRelease();
    }
}
