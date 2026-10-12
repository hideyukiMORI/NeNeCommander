using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Creates unique native wrappers whose references are owned by one STA call.</summary>
internal sealed partial class WindowsRecycleShell : IWindowsRecycleShell
{
    public IWindowsShellEntry OpenEntry(WindowsLocalPath path)
    {
        Guid iid = typeof(INativeWindowsShellEntry).GUID;
        Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(path.CanonicalText, IntPtr.Zero, in iid, out INativeWindowsShellEntry entry));
        return new WindowsShellEntry(entry);
    }

    public IWindowsShellOperation OpenOperation()
    {
        Guid clsid = new("3ad05575-8857-4850-9277-11b85bdb8e09");
        Guid iid = typeof(INativeWindowsShellOperation).GUID;
        Marshal.ThrowExceptionForHR(CoCreateInstance(in clsid, IntPtr.Zero, 1, in iid, out INativeWindowsShellOperation operation));
        return new WindowsShellOperation(operation);
    }

    public int QueryRecycleBin(WindowsLocalPath path)
    {
        byte[] information = new byte[24];
        BinaryPrimitives.WriteUInt32LittleEndian(information, 24);
        return SHQueryRecycleBin(path.Drive + @"\", information);
    }

    internal static void Release(object? value)
    {
        if (value is IDisposable owned)
        {
            owned.Dispose();
        }
    }

    [LibraryImport("shell32.dll", EntryPoint = "SHCreateItemFromParsingName", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHCreateItemFromParsingName(string path, IntPtr context, in Guid iid,
        [MarshalUsing(typeof(UniqueComInterfaceMarshaller<INativeWindowsShellEntry>))] out INativeWindowsShellEntry entry);

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid clsid, IntPtr outer, uint context, in Guid iid,
        [MarshalUsing(typeof(UniqueComInterfaceMarshaller<INativeWindowsShellOperation>))] out INativeWindowsShellOperation operation);

    [LibraryImport("shell32.dll", EntryPoint = "SHQueryRecycleBinW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHQueryRecycleBin(string root, [In, Out] byte[] information);
}
