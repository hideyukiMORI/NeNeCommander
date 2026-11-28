using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Opens existing transfer entries or creates protected new artifacts through one exact parent-relative native boundary.</summary>
internal static partial class WindowsTransferEntryOpen
{
    private const uint ReadAttributesAndSynchronize = 0x00100080;
    private const uint FileOpen = 1;
    private const uint FileCreate = 2;
    private const uint FileCreated = 2;
    private const uint ArtifactAccess = ReadAttributesAndSynchronize | 0x00030003;
    private const uint NonDirectory = 0x00000040;
    private const uint ShareRead = 1;
    private const uint NoFollowSynchronous = 0x00200020;
    private const int ObjectAttributesLength = 48;
    private const int UnicodeStringOffset = 48;
    private const int IoStatusOffset = 64;
    private const int NameOffset = 80;

    internal static SafeFileHandle OpenRoot(WindowsLocalPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.Parent is null
            ? OpenNative(IntPtr.Zero, @"\??\" + path.CanonicalText, new OpenParameters(ReadAttributesAndSynchronize, FileOpen, 0, IntPtr.Zero))
            : throw new ArgumentException("A transfer root must be a drive root.", nameof(path));
    }

    internal static SafeFileHandle OpenDirectory(SafeFileHandle parent, WindowsLocalPath parentPath, WindowsLocalPath path)
    {
        return OpenRelative(parent, ChildName(parentPath, path), ReadAttributesAndSynchronize);
    }

    internal static SafeFileHandle OpenFile(SafeFileHandle parent, WindowsLocalPath parentPath, WindowsLocalPath path, FileAccess access)
    {
        uint rights = access switch
        {
            FileAccess.Read => ReadAttributesAndSynchronize | 1,
            FileAccess.ReadWrite => ReadAttributesAndSynchronize | 3,
            FileAccess.Write => throw new ArgumentOutOfRangeException(nameof(access)),
            _ => throw new ArgumentOutOfRangeException(nameof(access))
        };
        return OpenRelative(parent, ChildName(parentPath, path), rights);
    }

    internal static SafeFileHandle CreateArtifact(
        WindowsTransferPathLease owner, WindowsLocalPath path, WindowsTransferSecurityDescriptor security)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(security);
        owner.Revalidate();
        string name = ChildName(owner.ParentPath, path);
        SafeFileHandle parent = owner.ParentHandle;
        WindowsFileIdentifier.RequireCaseInsensitiveDirectory(parent);
        bool parentReferenced = false;
        bool securityReferenced = false;
        try
        {
            parent.DangerousAddRef(ref parentReferenced);
            security.DangerousAddRef(ref securityReferenced);
            return OpenNative(parent.DangerousGetHandle(), name,
                new OpenParameters(ArtifactAccess, FileCreate, (uint)FileAttributes.Normal, security.DangerousGetHandle()));
        }
        finally
        {
            if (securityReferenced)
            {
                security.DangerousRelease();
            }
            if (parentReferenced)
            {
                parent.DangerousRelease();
            }
        }
    }

    private static string ChildName(WindowsLocalPath parent, WindowsLocalPath child)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);
        if (child.Parent != parent)
        {
            throw new ArgumentException("A transfer open requires one exact direct child.", nameof(child));
        }
        int offset = parent.CanonicalText.Length + (parent.Parent is null ? 0 : 1);
        return child.CanonicalText[offset..];
    }

    private static SafeFileHandle OpenRelative(SafeFileHandle parent, string name, uint rights)
    {
        ArgumentNullException.ThrowIfNull(parent);
        bool referenced = false;
        try
        {
            parent.DangerousAddRef(ref referenced);
            return OpenNative(parent.DangerousGetHandle(), name, new OpenParameters(rights, FileOpen, 0, IntPtr.Zero));
        }
        finally
        {
            if (referenced)
            {
                parent.DangerousRelease();
            }
        }
    }

    private static SafeFileHandle OpenNative(IntPtr parent, string name, OpenParameters parameters)
    {
        if (IntPtr.Size != 8)
        {
            throw new PlatformNotSupportedException("The transfer native ABI requires a 64-bit process.");
        }
        int allocationLength = checked(NameOffset + ((name.Length + 1) * sizeof(char)));
        IntPtr memory = Marshal.AllocHGlobal(allocationLength);
        try
        {
            Marshal.Copy(new byte[allocationLength], 0, memory, allocationLength);
            InitializeName(memory, parent, name);
            Marshal.WriteIntPtr(memory, 32, parameters.SecurityDescriptor);
            Marshal.WriteInt32(memory, 24, parameters.Disposition == FileCreate ? 0x40 : 0);
            uint options = NoFollowSynchronous | (parameters.Disposition == FileCreate ? NonDirectory : 0);
            int status = NtCreateFile(out SafeFileHandle handle, parameters.Rights, memory,
                IntPtr.Add(memory, IoStatusOffset), IntPtr.Zero, parameters.Attributes, ShareRead, parameters.Disposition,
                options, IntPtr.Zero, 0);
            if (status == 0 && !handle.IsInvalid &&
                (parameters.Disposition != FileCreate || Marshal.ReadInt64(memory, IoStatusOffset + 8) == FileCreated))
            {
                return handle;
            }
            handle.Dispose();
            throw new IOException("The transfer entry could not be opened.", new Win32Exception(RtlNtStatusToDosError(status)));
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
    }

    private static void InitializeName(IntPtr memory, IntPtr parent, string name)
    {
        IntPtr unicode = IntPtr.Add(memory, UnicodeStringOffset);
        IntPtr characters = IntPtr.Add(memory, NameOffset);
        ushort byteLength = checked((ushort)(name.Length * sizeof(char)));
        Marshal.WriteInt32(memory, ObjectAttributesLength);
        Marshal.WriteIntPtr(memory, 8, parent);
        Marshal.WriteIntPtr(memory, 16, unicode);
        Marshal.WriteInt16(unicode, unchecked((short)byteLength));
        Marshal.WriteInt16(unicode, 2, unchecked((short)byteLength));
        Marshal.WriteIntPtr(unicode, 8, characters);
        Marshal.Copy(name.ToCharArray(), 0, characters, name.Length);
    }

    private sealed record OpenParameters
    {
        internal OpenParameters(uint rights, uint disposition, uint attributes, IntPtr securityDescriptor)
        {
            Rights = rights;
            Disposition = disposition;
            Attributes = attributes;
            SecurityDescriptor = securityDescriptor;
        }

        internal uint Rights { get; }
        internal uint Disposition { get; }
        internal uint Attributes { get; }
        internal IntPtr SecurityDescriptor { get; }
    }

    [LibraryImport("ntdll.dll", EntryPoint = "NtCreateFile")]
    private static partial int NtCreateFile(out SafeFileHandle file, uint desiredAccess,
        IntPtr objectAttributes, IntPtr ioStatusBlock, IntPtr allocationSize, uint attributes,
        uint shareAccess, uint disposition, uint options, IntPtr extendedAttributes, uint extendedAttributesLength);

    [LibraryImport("ntdll.dll", EntryPoint = "RtlNtStatusToDosError")]
    private static partial int RtlNtStatusToDosError(int status);
}
