using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Infrastructure.Windows.Execution;

/// <summary>Owns one STA apartment until its awaited Shell operation releases every COM object.</summary>
internal sealed partial class WindowsShellIoScheduler : IWindowsLocalIoScheduler
{
    public Task<TResult> ScheduleAsync<TResult>(Func<TResult> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        Task<TResult> task = new(() => Execute(operation), CancellationToken.None, TaskCreationOptions.DenyChildAttach);
        Thread thread = new(() => task.RunSynchronously(TaskScheduler.Default));
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        return task;
    }

    private static TResult Execute<TResult>(Func<TResult> operation)
    {
        Marshal.ThrowExceptionForHR(CoInitializeEx(IntPtr.Zero, 2));
        try
        {
            return operation();
        }
        finally
        {
            CoUninitialize();
        }
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(IntPtr reserved, uint apartment);

    [LibraryImport("ole32.dll")]
    private static partial void CoUninitialize();
}
