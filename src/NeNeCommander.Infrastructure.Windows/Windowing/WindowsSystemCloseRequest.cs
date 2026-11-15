using System;
using System.Runtime.InteropServices;

namespace NeNeCommander.Infrastructure.Windows.Windowing;

/// <summary>
/// Asks the operating system to close one top-level window through its system close command, the
/// same <c>SC_CLOSE</c> request that the system menu and its keyboard equivalent issue. The window's
/// own close handling then runs, so a WinUI host observes it as <c>AppWindow.Closing</c> (ADR-0071).
/// The request is posted, not sent: it never runs the close handler on the caller's stack.
/// </summary>
public static partial class WindowsSystemCloseRequest
{
    private const uint SystemCommandMessage = 0x0112;
    private const nuint CloseCommand = 0xF060;

    /// <summary>Posts the system close command to the window; returns whether the system queued it.</summary>
    /// <param name="windowHandle">Handle of the caller's own top-level window; zero is refused.</param>
    public static bool Post(nint windowHandle)
    {
        // A zero handle would post to the calling thread's queue instead of to a window.
        ArgumentOutOfRangeException.ThrowIfZero(windowHandle);
        return PostMessage(windowHandle, SystemCommandMessage, CloseCommand, 0);
    }

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PostMessage(nint windowHandle, uint message, nuint wordParameter, nint longParameter);
}
