using System;
using System.IO;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns one opened entry and its initial identity without treating mutable directory metadata as lifetime identity.</summary>
internal sealed class WindowsTransferHeldEntry : IDisposable
{
    private WindowsTransferHeldEntry(WindowsLocalPath path, SafeFileHandle handle, VerifiedEntrySnapshot baseline)
    {
        Path = path;
        Handle = handle;
        Baseline = baseline;
    }

    internal WindowsLocalPath Path { get; }
    internal SafeFileHandle Handle { get; }
    internal VerifiedEntrySnapshot Baseline { get; }

    internal static WindowsTransferHeldEntry Take(WindowsLocalPath path, SafeFileHandle handle)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(handle);
        try
        {
            WindowsLocalHandleFacts facts = ReadAdmitted(handle);
            WindowsTransferPathObservation.Require(handle, path);
            VerifiedEntrySnapshot baseline = WindowsLocalEntryIdentity.CaptureHeld(path, facts);
            return new WindowsTransferHeldEntry(path, handle, baseline);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    internal void RequireDirectory()
    {
        WindowsLocalHandleFacts facts = ReadMatched();
        if (facts.Standard.Directory != 1)
        {
            throw new IOException("A transfer ancestor is not a directory.");
        }
    }

    internal void RequireOriginalFile()
    {
        WindowsLocalHandleFacts facts = ReadFileMatched();
        if (WindowsLocalEntryIdentity.CaptureHeld(Path, facts).Identity != Baseline.Identity)
        {
            throw new IOException("The original transfer file changed.");
        }
    }

    /// <summary>Requires the original held file object and path, without requiring pre-write length or timestamps.</summary>
    internal void RequireFileObject()
    {
        _ = ReadFileMatched();
    }

    private WindowsLocalHandleFacts ReadFileMatched()
    {
        WindowsLocalHandleFacts facts = ReadMatched();
        return facts.Standard.Directory != 0
            ? throw new WindowsOverwriteRefusalException(TransferOverwriteUnavailableReason.Folder, null) : facts;
    }

    private WindowsLocalHandleFacts ReadMatched()
    {
        WindowsLocalHandleFacts facts = ReadAdmitted(Handle);
        if (facts.ObjectIdentity != Baseline.ObjectIdentity)
        {
            throw new IOException("The held transfer entry identity changed.");
        }
        WindowsTransferPathObservation.Require(Handle, Path);
        return facts;
    }

    private static WindowsLocalHandleFacts ReadAdmitted(SafeFileHandle handle)
    {
        WindowsLocalHandleFacts facts = WindowsFileIdentifier.ReadLocalFacts(handle);
        TransferOverwriteUnavailableReason? refusal = !string.Equals(facts.FileSystemName, "NTFS", StringComparison.Ordinal)
            ? TransferOverwriteUnavailableReason.UnsupportedLocation
            : (facts.Basic.Attributes & (uint)FileAttributes.ReparsePoint) != 0
                ? TransferOverwriteUnavailableReason.SpecialAttributes
                : facts.Standard.DeletePending != 0 ? TransferOverwriteUnavailableReason.Unconfirmed : null;
        return refusal is not null ? throw new WindowsOverwriteRefusalException(refusal, null) : facts;
    }

    public void Dispose()
    {
        Handle.Dispose();
    }
}
