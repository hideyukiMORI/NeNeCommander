using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Preserves the native IFileOperation ABI; only recycle and owned test restoration are invoked.</summary>
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16, Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8")]
internal partial interface INativeWindowsShellOperation
{
    [PreserveSig]
    public int Advise(INativeWindowsRecycleSink sink, out uint cookie);
    [PreserveSig]
    public int Unadvise(uint cookie);
    [PreserveSig]
    public int SetOperationFlags(uint flags);
    [PreserveSig]
    public int SetProgressMessage(string message);
    [PreserveSig]
    public int SetProgressDialog(IntPtr dialog);
    [PreserveSig]
    public int SetProperties(IntPtr changes);
    [PreserveSig]
    public int SetOwnerWindow(IntPtr window);
    [PreserveSig]
    public int ApplyPropertiesToItem(INativeWindowsShellEntry entry);
    [PreserveSig]
    public int ApplyPropertiesToItems(IntPtr entries);
    [PreserveSig]
    public int RenameItem(INativeWindowsShellEntry entry, string name, INativeWindowsRecycleSink? sink);
    [PreserveSig]
    public int RenameItems(IntPtr entries, string name);
    [PreserveSig]
    public int MoveItem(INativeWindowsShellEntry entry, INativeWindowsShellEntry destination, string name, INativeWindowsRecycleSink? sink);
    [PreserveSig]
    public int MoveItems(IntPtr entries, INativeWindowsShellEntry destination);
    [PreserveSig]
    public int CopyItem(INativeWindowsShellEntry entry, INativeWindowsShellEntry destination, string name, INativeWindowsRecycleSink? sink);
    [PreserveSig]
    public int CopyItems(IntPtr entries, INativeWindowsShellEntry destination);
    [PreserveSig]
    public int DeleteItem(INativeWindowsShellEntry entry, INativeWindowsRecycleSink sink);
    [PreserveSig]
    public int DeleteItems(IntPtr entries);
    [PreserveSig]
    public int NewItem(INativeWindowsShellEntry destination, uint attributes, string name, string template, INativeWindowsRecycleSink? sink);
    [PreserveSig]
    public int PerformOperations();
    [PreserveSig]
    public int GetAnyOperationsAborted(out int aborted);
}
