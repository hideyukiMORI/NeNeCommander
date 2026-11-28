using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Requires a current exact normalized path and a local volume GUID observation from the held entry.</summary>
internal static partial class WindowsTransferPathObservation
{
    private const int PathCapacity = 32772;
    private const uint VolumeNameDos = 0;
    private const uint VolumeNameGuid = 1;

    internal static void Require(SafeFileHandle handle, WindowsLocalPath path)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(path);
        string actual = Read(handle, VolumeNameDos);
        string volume = Read(handle, VolumeNameGuid);
        if (!string.Equals(actual, @"\\?\" + path.CanonicalText, StringComparison.Ordinal) ||
            !HasVolumeGuid(volume))
        {
            throw new IOException("The transfer entry path or local volume is ambiguous.");
        }
    }

    internal static bool HasVolumeGuid(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.Length >= 49 && path.StartsWith(@"\\?\Volume{", StringComparison.Ordinal) &&
            path[47] == '}' && path[48] == '\\' && Guid.TryParseExact(path.AsSpan(11, 36), "D", out _);
    }

    private static string Read(SafeFileHandle handle, uint flags)
    {
        char[] buffer = new char[PathCapacity];
        uint length = GetFinalPathNameByHandle(handle, buffer, buffer.Length, flags);
        return length == 0
            ? throw new IOException("The transfer entry path could not be observed.", new Win32Exception(Marshal.GetLastWin32Error()))
            : length < buffer.Length && buffer[length] == '\0'
                ? new string(buffer, 0, checked((int)length))
                : throw new IOException("The transfer entry path observation exceeds its boundary.");
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint GetFinalPathNameByHandle(SafeFileHandle file,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
        [Out] char[] path, int capacity, uint flags);
}
