using System;
using NeNeCommander.Application.Input;

namespace NeNeCommander.App.Views;

/// <summary>Contains the sole mapped application intent forwarded by a WinUI input event.</summary>
public sealed class UserIntentMappedEventArgs : EventArgs
{
    internal UserIntentMappedEventArgs(UserIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        Intent = intent;
    }

    /// <summary>Gets the mapped application intent.</summary>
    public UserIntent Intent { get; }
}
