using System;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Represents the number of items a pending confirmation names.</summary>
public sealed record OperationItemCountDetail : OperationDetail
{
    internal OperationItemCountDetail(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        Count = count;
    }

    /// <summary>Gets the number of items the confirmation names.</summary>
    public int Count { get; }
}
