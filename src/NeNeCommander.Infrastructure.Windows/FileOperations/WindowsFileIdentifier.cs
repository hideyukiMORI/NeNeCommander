using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Reads stable volume and entry identifiers through one Windows namespace handle.</summary>
internal static partial class WindowsFileIdentifier
{
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const uint FileShareAll = 0x00000007;
    private const uint FileReadAttributes = 0x00000080;
    private const uint FileAttributeDirectory = 0x00000010;
    private const uint FileAttributeReparsePoint = 0x00000400;
    private const uint OpenExisting = 3;
    private const int FileIdInfoClass = 18;
    private const int FileIdInformationLength = 24;
    private const int FileBasicInfoClass = 0;
    private const int FileStandardInfoClass = 1;
    private const int FileAttributeTagInfoClass = 9;
    private const int FileInternalInformationClass = 6;
    private const int FileBasicInfoLength = 40;
    private const int FileStandardInfoLength = 24;
    private const int FileAttributeTagInfoLength = 8;
    private const int FileInternalInformationLength = 8;
    private const int IoStatusBlockLength = 16;
    private const int FileSystemNameLength = 261;
    private const int ErrorNotSupported = 50;
    private const string DirectoryKind = "directory";
    private const string FileKind = "file";
    private const string LinkKind = "link";
    private const string WslTokenVersion = "wsl-v2";

    // ADR-0049: the 9P redirector is the only Windows file system that exposes a WSL distribution,
    // and it supports none of the ADR-0033 identifier queries. The name therefore confirms the WSL
    // identity form selected from the validated provider, and the literal lives only here.
    private const string WslFileSystemName = "9P";

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
        return DescribeHandle(handle);
    }

    internal static string DescribeHandle(SafeFileHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        if (handle.IsInvalid)
        {
            throw CreateQueryFailure();
        }
        byte[] information = new byte[FileIdInformationLength];
        return GetFileInformationByHandleEx(handle, FileIdInfoClass, information, information.Length)
            ? Convert.ToHexString(information)
            : throw CreateQueryFailure();
    }

    /// <summary>
    /// Reads the ADR-0049 identity facts of one entry from its own no-follow handle without
    /// checking the file system. Only the guarded WSL reader below and deterministic tests that
    /// need real handle facts from an NTFS test root use this unguarded form.
    /// </summary>
    /// <param name="path">Existing namespace path of the entry to open.</param>
    /// <returns>The complete facts snapshot of the opened entry.</returns>
    internal static WslHandleFacts ReadHandleFacts(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using SafeFileHandle handle = OpenForAttributes(path);
        return ReadFacts(handle);
    }

    /// <summary>
    /// Reads the identity facts of one entry that must live on the WSL 9P file system. This is the
    /// only identity entry point the WSL file system uses, and any failed query closes the identity.
    /// </summary>
    /// <param name="path">Existing Windows-side WSL namespace path.</param>
    /// <returns>The complete facts snapshot of the opened entry.</returns>
    internal static WslHandleFacts ReadWslFacts(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using SafeFileHandle handle = OpenForAttributes(path);
        RequireWslFileSystem(ReadFileSystemName(handle));
        return ReadFacts(handle);
    }

    /// <summary>Closes the identity unless an opened handle reports exactly the 9P file system.</summary>
    /// <param name="fileSystemName">File system name reported for the opened handle.</param>
    internal static void RequireWslFileSystem(string fileSystemName)
    {
        ArgumentNullException.ThrowIfNull(fileSystemName);
        if (!string.Equals(fileSystemName, WslFileSystemName, StringComparison.Ordinal))
        {
            throw CreateQueryFailure(ErrorNotSupported);
        }
    }

    /// <summary>
    /// Composes the ADR-0049 <c>wsl-v2</c> identity token from one distribution name and one facts
    /// snapshot. A directory that reports a nonzero length is a provider anomaly and fails closed.
    /// </summary>
    /// <param name="distribution">Canonical distribution name of the validated WSL path.</param>
    /// <param name="facts">Facts read from the entry's own handle.</param>
    /// <returns>The opaque identity token text.</returns>
    internal static string ComposeWslToken(string distribution, WslHandleFacts facts)
    {
        ArgumentNullException.ThrowIfNull(distribution);
        ArgumentNullException.ThrowIfNull(facts);
        string kind = DescribeKind(facts);
        return string.Equals(kind, DirectoryKind, StringComparison.Ordinal) && facts.EndOfFile != 0
            ? throw CreateQueryFailure(ErrorNotSupported)
            : string.Join(
                '|',
                WslTokenVersion,
                distribution,
                facts.Inode.ToString(CultureInfo.InvariantCulture),
                kind,
                facts.ReparseTag.ToString("X8", CultureInfo.InvariantCulture),
                facts.NumberOfLinks.ToString(CultureInfo.InvariantCulture),
                facts.EndOfFile.ToString(CultureInfo.InvariantCulture),
                facts.LastWriteFileTimeUtc.ToString(CultureInfo.InvariantCulture),
                facts.ChangeFileTimeUtc.ToString(CultureInfo.InvariantCulture));
    }

    private static string DescribeKind(WslHandleFacts facts)
    {
        return (facts.Attributes & FileAttributeReparsePoint) != 0 && facts.ReparseTag != 0
            ? LinkKind
            : (facts.Attributes & FileAttributeDirectory) != 0 ? DirectoryKind : FileKind;
    }

    private static WslHandleFacts ReadFacts(SafeFileHandle handle)
    {
        long inode = ReadInode(handle);
        byte[] attributeTag = QueryInformation(handle, FileAttributeTagInfoClass, FileAttributeTagInfoLength);
        byte[] standard = QueryInformation(handle, FileStandardInfoClass, FileStandardInfoLength);
        byte[] basic = QueryInformation(handle, FileBasicInfoClass, FileBasicInfoLength);
        return new WslHandleFacts(
            inode,
            BitConverter.ToUInt32(attributeTag, 0),
            BitConverter.ToUInt32(attributeTag, 4),
            BitConverter.ToInt64(standard, 8),
            BitConverter.ToUInt32(standard, 16),
            BitConverter.ToInt64(basic, 16),
            BitConverter.ToInt64(basic, 24));
    }

    private static long ReadInode(SafeFileHandle handle)
    {
        byte[] information = new byte[FileInternalInformationLength];
        IntPtr statusBlock = Marshal.AllocHGlobal(IoStatusBlockLength);
        try
        {
            int status = NtQueryInformationFile(
                handle,
                statusBlock,
                information,
                information.Length,
                FileInternalInformationClass);
            return status == 0
                ? BitConverter.ToInt64(information, 0)
                : throw CreateQueryFailure(RtlNtStatusToDosError(status));
        }
        finally
        {
            Marshal.FreeHGlobal(statusBlock);
        }
    }

    private static byte[] QueryInformation(SafeFileHandle handle, int informationClass, int length)
    {
        byte[] information = new byte[length];
        return GetFileInformationByHandleEx(handle, informationClass, information, information.Length)
            ? information
            : throw CreateQueryFailure();
    }

    private static string ReadFileSystemName(SafeFileHandle handle)
    {
        // The call null-terminates the name inside the requested character count.
        char[] fileSystemName = new char[FileSystemNameLength];
        return GetVolumeInformationByHandle(
                handle,
                IntPtr.Zero,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                fileSystemName,
                fileSystemName.Length)
            ? new string(fileSystemName, 0, Array.IndexOf(fileSystemName, '\0'))
            : throw CreateQueryFailure();
    }

    private static SafeFileHandle OpenForAttributes(string path)
    {
        SafeFileHandle handle = CreateFile(
            path,
            FileReadAttributes,
            FileShareAll,
            IntPtr.Zero,
            OpenExisting,
            FileFlagBackupSemantics | FileFlagOpenReparsePoint,
            IntPtr.Zero);
        return handle.IsInvalid ? throw CreateQueryFailure() : handle;
    }

    private static IOException CreateQueryFailure()
    {
        return CreateQueryFailure(Marshal.GetLastWin32Error());
    }

    private static IOException CreateQueryFailure(int error)
    {
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

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "GetVolumeInformationByHandleW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetVolumeInformationByHandle(
        SafeFileHandle file,
        IntPtr volumeNameBuffer,
        int volumeNameSize,
        IntPtr volumeSerialNumber,
        IntPtr maximumComponentLength,
        IntPtr fileSystemFlags,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)]
        [Out] char[] fileSystemName,
        int fileSystemNameSize);

    [LibraryImport("ntdll.dll", EntryPoint = "NtQueryInformationFile")]
    private static partial int NtQueryInformationFile(
        SafeFileHandle file,
        IntPtr ioStatusBlock,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]
        [Out] byte[] fileInformation,
        int length,
        int informationClass);

    [LibraryImport("ntdll.dll", EntryPoint = "RtlNtStatusToDosError")]
    private static partial int RtlNtStatusToDosError(int status);
}
