using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Creates one empty entry exclusively without resolving a final reparse point.</summary>
internal static partial class WindowsEmptyFileCreation
{
    private const uint GenericWrite = 0x40000000;
    private const uint CreateNew = 1;
    private const uint NormalNoFollow = 0x00200080;

    internal static ProviderStepOutcome Create(string resolvedPath)
    {
        using SafeFileHandle handle = CreateFile(
            resolvedPath, GenericWrite, 0, IntPtr.Zero, CreateNew, NormalNoFollow, IntPtr.Zero);
        int error = Marshal.GetLastWin32Error();
        return handle.IsInvalid
            ? ProviderStepOutcome.Create(ProviderStepCompletion.Failed(Failure(resolvedPath, error)), ProviderStepReceipt.Empty)
            : ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty);
    }

    private static FileOperationFailureKind Failure(string resolvedPath, int error)
    {
        return error is 80 or 183 || (error == 5 && EntryExists(resolvedPath))
            ? FileOperationFailureKind.Conflict
            : WindowsFileFailureNormalizer.Normalize(unchecked((int)(0x80070000u | (uint)error)));
    }

    private static bool EntryExists(string resolvedPath)
    {
        try
        {
            _ = File.GetAttributes(resolvedPath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    private static partial SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);
}
