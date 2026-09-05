using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Reads the stable volume and entry identifiers for one Windows local directory entry.</summary>
internal static partial class WindowsLocalFileIdentifier
{
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const uint FileShareAll = 0x00000007;
    private const uint OpenExisting = 3;
    private const int FileIdInfoClass = 18;
    private const int FileIdInformationLength = 24;

    internal static string Describe(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using SafeFileHandle handle = CreateFile(
            path,
            0,
            FileShareAll,
            IntPtr.Zero,
            OpenExisting,
            FileFlagBackupSemantics | FileFlagOpenReparsePoint,
            IntPtr.Zero);
        return handle.IsInvalid
            ? throw CreateQueryFailure()
            : DescribeHandle(handle);
    }

    private static string DescribeHandle(SafeFileHandle handle)
    {
        byte[] information = new byte[FileIdInformationLength];
        return GetFileInformationByHandleEx(handle, FileIdInfoClass, information, information.Length)
            ? Convert.ToHexString(information)
            : throw CreateQueryFailure();
    }

    private static IOException CreateQueryFailure()
    {
        int error = Marshal.GetLastWin32Error();
        int hresult = unchecked((int)(0x80070000u | (uint)error));
        return new IOException(new Win32Exception(error).Message, hresult);
    }

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "CreateFileW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    private static partial SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [LibraryImport("kernel32.dll", EntryPoint = "GetFileInformationByHandleEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetFileInformationByHandleEx(
        SafeFileHandle file,
        int informationClass,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]
        [Out] byte[] fileInformation,
        int bufferSize);
}
