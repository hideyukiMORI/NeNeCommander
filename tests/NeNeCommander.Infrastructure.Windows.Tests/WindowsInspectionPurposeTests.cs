using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves purpose-specific scheduling and refusal of uninspected ordinary deletion.</summary>
[TestClass]
public sealed class WindowsInspectionPurposeTests
{
    /// <summary>Identity capture skips Shell while deletion keeps its existing Shell boundary.</summary>
    [TestMethod]
    [DataRow("Identity")]
    [DataRow("Deletion")]
    public async Task InspectAsyncWhenPurposeDiffersUsesOnlyRequiredSchedulerAndShellCalls(string choice)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = WindowsRecycleBinTests.Path(root.WriteFile("owned.txt", "kept"));
        CountingScheduler ordinary = new();
        CountingScheduler shellScheduler = new();
        ScriptedWindowsRecycleShell shell = new();
        WindowsLocalFileOperationAdapter adapter = new(new(ordinary, shellScheduler), new WindowsRecycleBin(shell), new DeterministicIdentifierSource());
        FileInspectionPurpose purpose = choice == "Deletion" ? FileInspectionPurpose.Deletion : FileInspectionPurpose.IdentityOnly;

        FileEntrySnapshot snapshot = Assert.IsInstanceOfType<FileInspectionSucceeded>(await adapter.InspectAsync(path, purpose, CancellationToken.None)).Snapshot;

        Assert.AreEqual(path, snapshot.Path);
        Assert.StartsWith("windows-v2|", snapshot.Identity.Value);
        Assert.AreSame(choice == "Deletion" ? DeletionCapability.Recycle : DeletionCapability.NotRequested, snapshot.DeletionCapability);
        Assert.AreEqual(choice == "Deletion" ? 0 : 1, ordinary.Calls);
        Assert.AreEqual(choice == "Deletion" ? 1 : 0, shellScheduler.Calls);
        Assert.AreEqual(choice == "Deletion" ? 1 : 0, shell.OpenEntryCount);
        Assert.AreEqual(choice == "Deletion" ? 1 : 0, shell.QueryCount);
        Assert.AreEqual("kept", File.ReadAllText(root.Resolve("owned.txt")));
    }

    /// <summary>An uninspected snapshot cannot authorize either ordinary deletion mode.</summary>
    [TestMethod]
    [DataRow("Permanent")]
    [DataRow("Recycle")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task DeleteAsyncWhenCapabilityWasNotRequestedRejectsBeforeScheduling(string choice)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot known = WindowsRecycleBinTests.Source(root);
        FileEntrySnapshot source = FileEntrySnapshot.Create(known.Path, known.Identity, DeletionCapability.NotRequested);
        CountingScheduler ordinary = new();
        CountingScheduler shellScheduler = new();
        ScriptedWindowsRecycleShell shell = new();
        WindowsLocalFileOperationAdapter adapter = new(new(ordinary, shellScheduler), new WindowsRecycleBin(shell), new DeterministicIdentifierSource());
        DeletionExecutionMode mode = choice == "Recycle" ? DeletionExecutionMode.Recycle : DeletionExecutionMode.Permanent;

        ProviderStepOutcome result = await adapter.DeleteAsync(source, mode, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Inspection, result.Failure);
        Assert.AreEqual(0, ordinary.Calls);
        Assert.AreEqual(0, shellScheduler.Calls);
        Assert.AreEqual(0, shell.OpenEntryCount);
        Assert.AreEqual(0, shell.QueryCount);
        Assert.IsTrue(File.Exists(root.Resolve("source")));
    }

    private sealed class CountingScheduler : IWindowsLocalIoScheduler
    {
        internal int Calls { get; private set; }

        public Task<TResult> ScheduleAsync<TResult>(Func<TResult> operation)
        {
            Calls++;
            return Task.FromResult(operation());
        }
    }
}
