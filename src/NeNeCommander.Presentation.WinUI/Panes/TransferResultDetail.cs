using System;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Reports separate transfer result counts without treating Skip as a filesystem effect.</summary>
public sealed record TransferResultDetail : OperationDetail
{
    private protected override void RestrictDerivation()
    {
    }

    internal TransferResultDetail(int notTransferred, int copied, int verified, SourceDeletionCounts sourceDeletion)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(notTransferred);
        ArgumentOutOfRangeException.ThrowIfNegative(copied);
        ArgumentOutOfRangeException.ThrowIfNegative(verified);
        ArgumentNullException.ThrowIfNull(sourceDeletion);
        NotTransferred = notTransferred;
        Copied = copied;
        Verified = verified;
        SourceDeletion = sourceDeletion;
    }

    /// <summary>Gets the explicit Skip count.</summary>
    public int NotTransferred { get; }
    /// <summary>Gets the completed copy count.</summary>
    public int Copied { get; }
    /// <summary>Gets the verified target count.</summary>
    public int Verified { get; }
    /// <summary>Gets the source deletion count.</summary>
    public int SourceDeleted => SourceDeletion.Deleted;
    /// <summary>Gets the count of sources whose descendants were only partly removed.</summary>
    public int SourcePartiallyDeleted => SourceDeletion.PartiallyDeleted;
    /// <summary>Gets the separate complete and partial source-removal counts.</summary>
    public SourceDeletionCounts SourceDeletion { get; }
}
