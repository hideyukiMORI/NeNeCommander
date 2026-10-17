using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Lifecycle;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves a normal close has one owned drain before its final callback.</summary>
[TestClass]
public sealed class WindowShutdownOwnerTests
{
    /// <summary>Proves a pending drain keeps the close callback behind its barrier.</summary>
    [TestMethod]
    public async Task RequestCloseWhenDrainPendingWaitsAndRejectsDuplicates()
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<string> events = [];
        WindowShutdownOwner owner = new(
            () =>
            {
                events.Add("drain-started");
                return release.Task;
            },
            () =>
            {
                events.Add("closed");
                closed.SetResult();
            },
            _ => Assert.Fail("No defect expected."));

        bool first = owner.RequestClose();
        bool duplicate = owner.RequestClose();
        Assert.HasCount(1, events);
        Assert.AreEqual("drain-started", events[0]);
        release.SetResult();
        await closed.Task;
        bool afterCompletion = owner.RequestClose();

        Assert.IsTrue(first);
        Assert.IsFalse(duplicate);
        Assert.IsFalse(afterCompletion);
        Assert.HasCount(2, events);
        Assert.AreEqual("closed", events[1]);
    }

    /// <summary>Proves a synchronous drain closes once and callback reentrancy cannot restart it.</summary>
    [TestMethod]
    public void RequestCloseWhenDrainCompletesSynchronouslyRejectsReentrantClose()
    {
        int drains = 0;
        int closes = 0;
        bool reentrant = true;
        Func<bool> repeat = () => true;
        WindowShutdownOwner owner = new(
            () =>
            {
                drains++;
                return Task.CompletedTask;
            },
            () =>
            {
                closes++;
                reentrant = repeat();
            },
            _ => Assert.Fail("No defect expected."));
        repeat = owner.RequestClose;

        bool first = owner.RequestClose();
        bool duplicate = owner.RequestClose();

        Assert.IsTrue(first);
        Assert.IsFalse(reentrant);
        Assert.IsFalse(duplicate);
        Assert.AreEqual(1, drains);
        Assert.AreEqual(1, closes);
    }

    /// <summary>Proves a fault after a pending drain is observed once without normal close.</summary>
    [TestMethod]
    public async Task RequestCloseWhenDrainFaultsObservesExactDefectWithoutClosing()
    {
        InvalidOperationException defect = new("Injected drain defect.");
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<Exception> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int observations = 0;
        int closes = 0;
        WindowShutdownOwner owner = new(
            () => release.Task,
            () => closes++,
            exception =>
            {
                observations++;
                observed.SetResult(exception);
            });

        bool first = owner.RequestClose();
        release.SetException(defect);
        Exception actual = await observed.Task;
        bool duplicate = owner.RequestClose();

        Assert.IsTrue(first);
        Assert.IsFalse(duplicate);
        Assert.AreSame(defect, actual);
        Assert.AreEqual(1, observations);
        Assert.AreEqual(0, closes);
    }

    /// <summary>Proves a synchronous drain defect follows the same owned observation path.</summary>
    [TestMethod]
    public async Task RequestCloseWhenDrainThrowsSynchronouslyObservesExactDefectWithoutClosing()
    {
        InvalidOperationException defect = new("Synchronous drain defect.");
        TaskCompletionSource<Exception> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int closes = 0;
        WindowShutdownOwner owner = new(
            () => throw defect,
            () => closes++,
            observed.SetResult);

        bool first = owner.RequestClose();
        Exception actual = await observed.Task;

        Assert.IsTrue(first);
        Assert.AreSame(defect, actual);
        Assert.AreEqual(0, closes);
        Assert.IsFalse(owner.RequestClose());
    }

    /// <summary>Proves a final-close defect is reported by the same owner exactly once.</summary>
    [TestMethod]
    public async Task RequestCloseWhenFinalCloseThrowsObservesExactDefect()
    {
        InvalidOperationException defect = new("Injected close defect.");
        TaskCompletionSource<Exception> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int closes = 0;
        int observations = 0;
        WindowShutdownOwner owner = new(
            static () => Task.CompletedTask,
            () =>
            {
                closes++;
                throw defect;
            },
            exception =>
            {
                observations++;
                observed.SetResult(exception);
            });

        bool first = owner.RequestClose();
        Exception actual = await observed.Task;

        Assert.IsTrue(first);
        Assert.AreSame(defect, actual);
        Assert.AreEqual(1, closes);
        Assert.AreEqual(1, observations);
        Assert.IsFalse(owner.RequestClose());
    }
}
