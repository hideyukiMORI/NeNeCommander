using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Resolves the mounted-volume identity Windows reports for existing local paths so atomic-move
/// capability is never inferred from a drive-letter prefix.
/// </summary>
internal static partial class WindowsLocalVolumeIdentity
{
    private const int MaximumWindowsPathLength = 32768;

    internal static bool SharesVolume(string sourcePath, string destinationPath)
    {
        string sourceVolume = ResolveVolumeName(sourcePath);
        string destinationVolume = ResolveVolumeName(destinationPath);
        return sourceVolume.Equals(destinationVolume, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveVolumeName(string path)
    {
        char[] volumePath = new char[MaximumWindowsPathLength];
        if (!GetVolumePathName(path, volumePath, volumePath.Length))
        {
            throw CreateQueryFailure();
        }

        char[] volumeName = new char[MaximumWindowsPathLength];
        return GetVolumeNameForVolumeMountPoint(ReadBuffer(volumePath), volumeName, volumeName.Length)
            ? ReadBuffer(volumeName)
            : throw CreateQueryFailure();
    }

    internal static string ReadBuffer(char[] buffer)
    {
        int terminator = Array.IndexOf(buffer, '\0');
        return new string(buffer, 0, terminator < 0 ? buffer.Length : terminator);
    }

    private static IOException CreateQueryFailure()
    {
        return new IOException(
            "Windows volume identity query failed.",
            new Win32Exception(Marshal.GetLastWin32Error()));
    }

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "GetVolumePathNameW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetVolumePathName(
        string fileName,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
        [Out] char[] volumePathName,
        int bufferLength);

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "GetVolumeNameForVolumeMountPointW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetVolumeNameForVolumeMountPoint(
        string volumeMountPoint,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
        [Out] char[] volumeName,
        int bufferLength);
}
