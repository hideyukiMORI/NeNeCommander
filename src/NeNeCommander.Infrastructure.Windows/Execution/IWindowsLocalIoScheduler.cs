using System;
using System.Threading.Tasks;

namespace NeNeCommander.Infrastructure.Windows.Execution;

internal interface IWindowsLocalIoScheduler
{
    public Task<TResult> ScheduleAsync<TResult>(Func<TResult> operation);
}
