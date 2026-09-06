using System;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Reports separate transfer result counts without treating Skip as a filesystem effect.</summary>
public sealed record TransferResultDetail : OperationDetail
{
    internal TransferResultDetail(int notTransferred, int copied, int verified, int sourceDeleted)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(notTransferred);
        ArgumentOutOfRangeException.ThrowIfNegative(copied);
        ArgumentOutOfRangeException.ThrowIfNegative(verified);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceDeleted);
        NotTransferred = notTransferred;
        Copied = copied;
        Verified = verified;
        SourceDeleted = sourceDeleted;
    }

    /// <summary>Gets the explicit Skip count.</summary>
    public int NotTransferred { get; }
    /// <summary>Gets the completed copy count.</summary>
    public int Copied { get; }
    /// <summary>Gets the verified target count.</summary>
    public int Verified { get; }
    /// <summary>Gets the source deletion count.</summary>
    public int SourceDeleted { get; }
}
