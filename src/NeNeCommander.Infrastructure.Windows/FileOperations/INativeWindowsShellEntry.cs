using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Preserves the native IShellItem ABI inside the Shell adapter.</summary>
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16, Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
internal partial interface INativeWindowsShellEntry
{
    [PreserveSig]
    public int BindToHandler(IntPtr context, in Guid handler, in Guid iid, out IntPtr result);

    [PreserveSig]
    public int GetParent([MarshalUsing(typeof(UniqueComInterfaceMarshaller<INativeWindowsShellEntry>))] out INativeWindowsShellEntry? parent);

    [PreserveSig]
    public int GetDisplayName(uint form, out string name);

    [PreserveSig]
    public int GetAttributes(uint mask, out uint attributes);

    [PreserveSig]
    public int Compare(INativeWindowsShellEntry other, uint hint, out int order);
}
