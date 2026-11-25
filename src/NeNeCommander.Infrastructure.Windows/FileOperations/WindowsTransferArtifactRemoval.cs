using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Removes only the link bound to one synchronously opened artifact handle. The caller must serialize one use with no outstanding I/O, freshly admit ownership and record uncertainty; no path, retry or cleanup policy is accepted here.</summary>
internal sealed partial class WindowsTransferArtifactRemoval
{
    internal WindowsTransferArtifactRemoval(Func<IntPtr, int> requestDisposition, Func<IntPtr, bool> close)
    {
        ArgumentNullException.ThrowIfNull(requestDisposition);
        ArgumentNullException.ThrowIfNull(close);
        RequestDisposition = requestDisposition;
        Close = close;
    }

    internal static WindowsTransferArtifactRemoval Native { get; } = new(RequestNativeDisposition, CloseHandle);

    internal Func<IntPtr, int> RequestDisposition { get; }
    internal Func<IntPtr, bool> Close { get; }

    /// <summary>Returns only after disposition and close acknowledgement, invalidating the consumed SafeFileHandle before releasing its protective reference. A failure conveys no removal acknowledgement.</summary>
    internal void Remove(SafeFileHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ObjectDisposedException.ThrowIf(handle.IsClosed, handle);
        if (handle.IsInvalid)
        {
            throw new ArgumentException("Artifact removal requires a live owned file handle.", nameof(handle));
        }
        bool referenced = false;
        try
        {
            handle.DangerousAddRef(ref referenced);
            IntPtr native = handle.DangerousGetHandle();
            int status = RequestDisposition(native);
            if (status != 0)
            {
                throw new IOException("The held artifact disposition was not acknowledged.", new Win32Exception(RtlNtStatusToDosError(status)));
            }
            if (!Close(native))
            {
                throw new IOException("The held artifact handle close was not acknowledged.", new Win32Exception(Marshal.GetLastPInvokeError()));
            }
            handle.SetHandleAsInvalid();
        }
        finally
        {
            if (referenced)
            {
                handle.DangerousRelease();
            }
        }
    }

    private static int RequestNativeDisposition(IntPtr handle)
    {
        if (IntPtr.Size != 8)
        {
            throw new PlatformNotSupportedException("The artifact disposition ABI requires a 64-bit process.");
        }
        IntPtr memory = Marshal.AllocHGlobal(20);
        try
        {
            Marshal.Copy(new byte[20], 0, memory, 20);
            Marshal.WriteInt32(memory, 16, 3);
            return NtSetInformationFile(handle, memory, IntPtr.Add(memory, 16), sizeof(uint), 64);
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
    }

    [LibraryImport("ntdll.dll", EntryPoint = "NtSetInformationFile")]
    private static partial int NtSetInformationFile(IntPtr file, IntPtr ioStatusBlock, IntPtr fileInformation, uint length, int informationClass);

    [LibraryImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr handle);

    [LibraryImport("ntdll.dll", EntryPoint = "RtlNtStatusToDosError")]
    private static partial int RtlNtStatusToDosError(int status);
}