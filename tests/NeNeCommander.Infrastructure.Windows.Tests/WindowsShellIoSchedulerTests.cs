using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves Shell executes on an owned STA and faults remain observed by its awaiter.</summary>
[TestClass]
public sealed class WindowsShellIoSchedulerTests
{
    /// <summary>The caller's apartment cannot decide the Shell operation's apartment.</summary>
    [TestMethod]
    public async Task ScheduleAsyncWhenCalledRunsOnSeparateStaThreadAsync()
    {
        Thread caller = Thread.CurrentThread;
        WindowsShellIoScheduler scheduler = new();
        ApartmentState apartment = await scheduler.ScheduleAsync(ReadApartment);
        Thread worker = await scheduler.ScheduleAsync(ReadThread);
        Assert.AreEqual(ApartmentState.STA, apartment);
        Assert.AreNotSame(caller, worker);
    }

    /// <summary>Unexpected defects fault the awaited task instead of escaping its owned thread.</summary>
    [TestMethod]
    public async Task ScheduleAsyncWhenOperationThrowsPreservesExceptionAsync()
    {
        WindowsShellIoScheduler scheduler = new();
        InvalidOperationException expected = new("Owned defect.");
        InvalidOperationException observed = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => scheduler.ScheduleAsync<int>(() => throw expected));
        Assert.AreSame(expected, observed);
    }

    private static ApartmentState ReadApartment()
    {
        return Thread.CurrentThread.GetApartmentState();
    }

    private static Thread ReadThread()
    {
        return Thread.CurrentThread;
    }
}
