using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves capability collection can advance without dispatching effects outside their owner.</summary>
[TestClass]
public sealed class GatewayCapabilityContinuationTests
{
    /// <summary>Proves pending capability answers do not require an owner turn between queries.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCapabilitiesAreDeferredQueriesWithoutOwnerThenReturnsForEffects()
    {
        CapabilityOwnerContext owner = new();
        DeferredCapabilityPort port = new();
        CapabilityOwnerProgress progress = new();
        using FileOperationGateway gateway = new(port);
        Task<FileOperationOutcome> operation = owner.StartAsync(
            () => gateway.ExecuteAsync(port.Request, progress, CancellationToken.None));

        FileOperationOutcome reentrant = await gateway.ExecuteAsync(port.Request, progress, CancellationToken.None);
        bool advancedWithoutOwner = await AdvanceToSecondAsync(owner, port);
        int callsBeforeSecondAnswer = port.Calls.Count;
        int reportsBeforeSecondAnswer = progress.Reports.Count;
        port.Complete(1, AtomicMoveCapabilityOutcome.Supported);
        await owner.Posted;
        int callsBeforeOwner = port.Calls.Count;
        int reportsBeforeOwner = progress.Reports.Count;
        owner.Advance();
        FileOperationOutcome outcome = await operation;

        Assert.IsTrue(advancedWithoutOwner);
        Assert.AreSame(FileOperationFailureKind.Reentrant, reentrant.Failure);
        Assert.AreEqual(2, callsBeforeSecondAnswer);
        Assert.AreEqual(0, reportsBeforeSecondAnswer);
        Assert.AreEqual(2, callsBeforeOwner);
        Assert.AreEqual(0, reportsBeforeOwner);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(2, outcome.Effects);
        Assert.AreSame(owner, port.Calls[0].Context);
        Assert.AreNotSame(owner, port.Calls[1].Context);
        AssertOwnedEffects(owner, port, progress);
    }

    /// <summary>Proves cancellation after a pending query prevents any later query or effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenPendingCapabilityIsCancelledStopsBeforeNextQuery()
    {
        CapabilityOwnerContext owner = new();
        DeferredCapabilityPort port = new();
        CapabilityOwnerProgress progress = new();
        using FileOperationGateway gateway = new(port);
        using CancellationTokenSource cancellation = new();
        Task<FileOperationOutcome> operation = owner.StartAsync(
            () => gateway.ExecuteAsync(port.Request, progress, cancellation.Token));

        cancellation.Cancel();
        port.Complete(0, AtomicMoveCapabilityOutcome.Supported);
        await owner.Posted;
        owner.Advance();
        FileOperationOutcome outcome = await operation;

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(1, port.Calls);
        Assert.IsFalse(port.SecondStarted.IsCompleted);
        Assert.IsEmpty(outcome.Effects);
        Assert.IsEmpty(progress.Reports);
    }

    /// <summary>Proves an asynchronously rejected later capability refuses the complete batch.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenLaterPendingCapabilityFailsStartsNoMutation()
    {
        CapabilityOwnerContext owner = new();
        DeferredCapabilityPort port = new();
        CapabilityOwnerProgress progress = new();
        using FileOperationGateway gateway = new(port);
        Task<FileOperationOutcome> operation = owner.StartAsync(
            () => gateway.ExecuteAsync(port.Request, progress, CancellationToken.None));

        _ = await AdvanceToSecondAsync(owner, port);
        port.Complete(1, AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.Inspection));
        await owner.Posted;
        owner.Advance();
        FileOperationOutcome outcome = await operation;

        Assert.AreSame(FileOperationFailureKind.Inspection, outcome.Failure);
        Assert.HasCount(2, port.Calls);
        Assert.IsEmpty(outcome.Effects);
        Assert.IsEmpty(progress.Reports);
    }

    /// <summary>Proves deferred defects propagate unchanged and release the gateway lease.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenPendingCapabilityFaultsPreservesExceptionAndReleasesLease()
    {
        CapabilityOwnerContext owner = new();
        DeferredCapabilityPort port = new();
        CapabilityOwnerProgress progress = new();
        using FileOperationGateway gateway = new(port);
        InvalidOperationException expected = new("capability defect");
        Task<FileOperationOutcome> operation = owner.StartAsync(
            () => gateway.ExecuteAsync(port.Request, progress, CancellationToken.None));

        port.Fault(0, expected);
        await owner.Posted;
        owner.Advance();
        InvalidOperationException actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => operation);
        port.RejectNextInspection();
        FileOperationOutcome subsequent = await gateway.ExecuteAsync(port.Request, progress, CancellationToken.None);

        Assert.AreSame(expected, actual);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, subsequent.Failure);
        Assert.HasCount(1, port.Calls);
        Assert.IsEmpty(progress.Reports);
    }

    /// <summary>Proves already-complete answers do not force a thread or context switch.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCapabilitiesAlreadyCompletedKeepsSynchronousOwner()
    {
        CapabilityOwnerContext owner = new();
        DeferredCapabilityPort port = new();
        CapabilityOwnerProgress progress = new();
        using FileOperationGateway gateway = new(port);
        port.Complete(0, AtomicMoveCapabilityOutcome.Supported);
        port.Complete(1, AtomicMoveCapabilityOutcome.Supported);

        Task<FileOperationOutcome> operation = owner.StartAsync(
            () => gateway.ExecuteAsync(port.Request, progress, CancellationToken.None));
        bool completedInline = operation.IsCompletedSuccessfully;
        FileOperationOutcome outcome = await operation;

        Assert.IsTrue(completedInline);
        Assert.IsFalse(owner.Posted.IsCompleted);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.IsTrue(port.Calls.All(call => ReferenceEquals(owner, call.Context)));
        AssertOwnedEffects(owner, port, progress);
    }

    private static async Task<bool> AdvanceToSecondAsync(CapabilityOwnerContext owner, DeferredCapabilityPort port)
    {
        port.Complete(0, AtomicMoveCapabilityOutcome.Supported);
        Task firstSignal = await Task.WhenAny(port.SecondStarted, owner.Posted);
        bool advancedWithoutOwner = firstSignal == port.SecondStarted;
        if (!advancedWithoutOwner)
        {
            owner.Advance();
        }
        await port.SecondStarted;
        return advancedWithoutOwner;
    }

    private static void AssertOwnedEffects(
        CapabilityOwnerContext owner,
        DeferredCapabilityPort port,
        CapabilityOwnerProgress progress)
    {
        string[] expected = ["Capability:C:\\first", "Capability:C:\\second", "Move:C:\\first", "Move:C:\\second"];
        CollectionAssert.AreEqual(expected, port.Calls.Select(call => call.Call).ToArray());
        Assert.IsTrue(port.Calls.Skip(2).All(call => ReferenceEquals(owner, call.Context)));
        Assert.HasCount(2, progress.Reports);
        Assert.IsTrue(progress.Reports.All(report => ReferenceEquals(owner, report.Context)));
        Assert.AreEqual(FileOperationProgress.Create(1, 2), progress.Reports[0].Progress);
        Assert.AreEqual(FileOperationProgress.Create(2, 2), progress.Reports[1].Progress);
    }
}
