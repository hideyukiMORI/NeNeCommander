using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Preserves the IFileOperationProgressSink callback ABI without exposing Shell to Application.</summary>
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("04b0f1a7-9490-44bc-96e1-4296a31252e2")]
internal partial interface INativeWindowsRecycleSink
{
    [PreserveSig]
    public int StartOperations();
    [PreserveSig]
    public int FinishOperations(int result);
    [PreserveSig]
    public int PreRenameItem(uint flags, IntPtr entry, string name);
    [PreserveSig]
    public int PostRenameItem(uint flags, IntPtr entry, string name, int result, IntPtr created);
    [PreserveSig]
    public int PreMoveItem(uint flags, IntPtr entry, IntPtr destination, string name);
    [PreserveSig]
    public int PostMoveItem(uint flags, IntPtr entry, IntPtr destination, string name, int result, IntPtr created);
    [PreserveSig]
    public int PreCopyItem(uint flags, IntPtr entry, IntPtr destination, string name);
    [PreserveSig]
    public int PostCopyItem(uint flags, IntPtr entry, IntPtr destination, string name, int result, IntPtr created);
    [PreserveSig]
    public int PreDeleteItem(uint flags, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<INativeWindowsShellEntry>))] INativeWindowsShellEntry entry);
    [PreserveSig]
    public int PostDeleteItem(uint flags, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<INativeWindowsShellEntry>))] INativeWindowsShellEntry entry, int result,
        [MarshalUsing(typeof(UniqueComInterfaceMarshaller<INativeWindowsShellEntry>))] INativeWindowsShellEntry? created);
    [PreserveSig]
    public int PreNewItem(uint flags, IntPtr destination, string name);
    [PreserveSig]
    public int PostNewItem(uint flags, IntPtr destination, string name, string template, uint attributes, int result, IntPtr created);
    [PreserveSig]
    public int UpdateProgress(uint total, uint completed);
    [PreserveSig]
    public int ResetTimer();
    [PreserveSig]
    public int PauseTimer();
    [PreserveSig]
    public int ResumeTimer();
}
