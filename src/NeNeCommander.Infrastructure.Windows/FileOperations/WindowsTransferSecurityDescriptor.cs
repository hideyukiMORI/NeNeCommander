using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns the initial protected artifact descriptor, granting access only to the captured current user.</summary>
internal sealed class WindowsTransferSecurityDescriptor : SafeHandle
{
    private WindowsTransferSecurityDescriptor(IntPtr descriptor)
        : base(IntPtr.Zero, true)
    {
        SetHandle(descriptor);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    internal static WindowsTransferSecurityDescriptor CaptureCurrent()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        SecurityIdentifier user = identity.User ?? throw new IOException("The current user has no security identifier.");
        FileSecurity security = new();
        security.SetOwner(user);
        security.SetAccessRuleProtection(true, false);
        security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
        byte[] bytes = security.GetSecurityDescriptorBinaryForm();
        IntPtr memory = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, memory, bytes.Length);
            WindowsTransferSecurityDescriptor descriptor = new(memory);
            memory = IntPtr.Zero;
            return descriptor;
        }
        finally
        {
            if (memory != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(memory);
            }
        }
    }

    protected override bool ReleaseHandle()
    {
        Marshal.FreeHGlobal(handle);
        return true;
    }
}
