using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents a permanent deletion the gateway refused until the exact source set is confirmed.
/// Only <see cref="Input.UserIntent.Confirm"/> and <see cref="Input.UserIntent.Escape"/> leave this state.
/// </summary>
public sealed record OperationAwaitingConfirmation : OperationActivity
{
    internal OperationAwaitingConfirmation(DeleteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Request = request;
    }

    /// <summary>Gets the unconfirmed request whose frozen sources the confirmation will name.</summary>
    public DeleteRequest Request { get; }
}
