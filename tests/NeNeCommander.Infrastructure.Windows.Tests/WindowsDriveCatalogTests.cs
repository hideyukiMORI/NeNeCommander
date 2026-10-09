using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Drives;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves the drive catalog's classification, readiness, root validation, and failure policy with a
/// deterministic volume fixture; the volumes of the machine running the tests are never consulted.
/// </summary>
[TestClass]
public sealed class WindowsDriveCatalogTests
{
    /// <summary>Proves listing runs only when the shared I/O boundary executes it.</summary>
    [TestMethod]
    public async Task ListAsyncWhenScheduledRunsThroughTheIoBoundaryAsync()
    {
        ScriptedDriveEnumerator enumerator = new([new WindowsDriveSnapshot("C:\\", DriveType.Fixed, WindowsDriveReadiness.Ready)]);
        ManualIoScheduler scheduler = new();
        WindowsDriveCatalog catalog = new(new WindowsLocalIoExecutionBoundary(scheduler), enumerator);

        Task<DriveCatalogOutcome> listing = catalog.ListAsync(CancellationToken.None);

        Assert.IsFalse(listing.IsCompleted);
        Assert.AreEqual(0, enumerator.EnumerateCount);
        Assert.AreEqual(1, scheduler.PendingCount);
        scheduler.ExecuteAll();
        DriveCatalogSucceeded succeeded = Assert.IsInstanceOfType<DriveCatalogSucceeded>(await listing);
        Assert.AreEqual(1, enumerator.EnumerateCount);
        Assert.HasCount(1, succeeded.Drives);
    }

    /// <summary>Proves an already cancelled request schedules nothing and touches no volume.</summary>
    [TestMethod]
    public async Task ListAsyncWhenAlreadyCancelledReturnsCancelledWithoutWorkAsync()
    {
        ScriptedDriveEnumerator enumerator = new([]);
        ManualIoScheduler scheduler = new();
        WindowsDriveCatalog catalog = new(new WindowsLocalIoExecutionBoundary(scheduler), enumerator);

        DriveCatalogOutcome outcome = await catalog.ListAsync(new CancellationToken(true));

        _ = Assert.IsInstanceOfType<DriveCatalogCancelled>(outcome);
        Assert.AreEqual(0, scheduler.PendingCount);
        Assert.AreEqual(0, enumerator.EnumerateCount);
    }

    /// <summary>Proves each ready volume type maps to its closed kind and keeps provider order.</summary>
    [TestMethod]
    public async Task ListAsyncWhenVolumesAreReadyClassifiesEveryTypeAsync()
    {
        ScriptedDriveEnumerator enumerator = new(
        [
            new WindowsDriveSnapshot("C:\\", DriveType.Fixed, WindowsDriveReadiness.Ready),
            new WindowsDriveSnapshot("E:\\", DriveType.Removable, WindowsDriveReadiness.Ready),
            new WindowsDriveSnapshot("Z:\\", DriveType.Network, WindowsDriveReadiness.Ready),
            new WindowsDriveSnapshot("F:\\", DriveType.CDRom, WindowsDriveReadiness.Ready),
            new WindowsDriveSnapshot("R:\\", DriveType.Ram, WindowsDriveReadiness.Ready),
            new WindowsDriveSnapshot("N:\\", DriveType.NoRootDirectory, WindowsDriveReadiness.Ready),
            new WindowsDriveSnapshot("U:\\", DriveType.Unknown, WindowsDriveReadiness.Ready),
        ]);

        DriveCatalogSucceeded succeeded = await ListAsync(enumerator);

        DriveKind[] expected =
        [
            DriveKind.Fixed,
            DriveKind.Removable,
            DriveKind.Network,
            DriveKind.Optical,
            DriveKind.Unknown,
            DriveKind.Unknown,
            DriveKind.Unknown,
        ];
        Assert.HasCount(expected.Length, succeeded.Drives);
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.AreSame(expected[index], succeeded.Drives[index].Kind);
        }
        Assert.AreEqual("C:\\", succeeded.Drives[0].Root.CanonicalText);
        Assert.AreEqual("U:\\", succeeded.Drives[6].Root.CanonicalText);
    }

    /// <summary>Proves an integer outside the framework enumeration is an unknown kind.</summary>
    [TestMethod]
    public void ClassifyWhenTypeIsUndeclaredReturnsUnknown()
    {
        Assert.AreSame(DriveKind.Unknown, WindowsDriveCatalog.Classify((DriveType)99));
    }

    /// <summary>Proves a ready volume reports its label and a not-ready volume stays listed as unknown without a label read.</summary>
    [TestMethod]
    public async Task ListAsyncWhenVolumeIsNotReadyListsItAsUnknownWithoutReadingItsLabelAsync()
    {
        ScriptedDriveEnumerator enumerator = new(
        [
            new WindowsDriveSnapshot("C:\\", DriveType.Fixed, WindowsDriveReadiness.Ready),
            new WindowsDriveSnapshot("D:\\", DriveType.CDRom, WindowsDriveReadiness.NotReady),
        ]);
        enumerator.Labels["C:\\"] = "System";
        enumerator.Labels["D:\\"] = "Disc";

        DriveCatalogSucceeded succeeded = await ListAsync(enumerator);

        Assert.AreEqual("System", succeeded.Drives[0].VolumeLabel);
        Assert.AreSame(DriveKind.Unknown, succeeded.Drives[1].Kind);
        Assert.IsNull(succeeded.Drives[1].VolumeLabel);
        Assert.HasCount(1, enumerator.LabelReads);
        Assert.AreEqual("C:\\", enumerator.LabelReads[0]);
    }

    /// <summary>Proves an empty label is absence and a label read that fails keeps the volume listed.</summary>
    [TestMethod]
    [DataRow("empty")]
    [DataRow("io")]
    [DataRow("access")]
    public async Task ListAsyncWhenLabelIsEmptyOrUnreadableListsTheVolumeWithoutLabelAsync(string labelCase)
    {
        ScriptedDriveEnumerator enumerator = new([new WindowsDriveSnapshot("E:\\", DriveType.Removable, WindowsDriveReadiness.Ready)]);
        if (labelCase == "empty")
        {
            enumerator.Labels["E:\\"] = string.Empty;
        }
        else
        {
            enumerator.LabelFailure = labelCase == "io" ? new DriveNotFoundException() : new UnauthorizedAccessException();
        }

        DriveCatalogSucceeded succeeded = await ListAsync(enumerator);

        Assert.HasCount(1, succeeded.Drives);
        Assert.AreSame(DriveKind.Removable, succeeded.Drives[0].Kind);
        Assert.IsNull(succeeded.Drives[0].VolumeLabel);
    }

    /// <summary>Proves a reported root that is not a drive root is counted, not shown, and a repeated root is listed once.</summary>
    [TestMethod]
    public async Task ListAsyncWhenRootIsUnrepresentableCountsItAsync()
    {
        ScriptedDriveEnumerator enumerator = new(
        [
            new WindowsDriveSnapshot("C:\\", DriveType.Fixed, WindowsDriveReadiness.Ready),
            new WindowsDriveSnapshot("C:\\Mount", DriveType.Fixed, WindowsDriveReadiness.Ready),
            new WindowsDriveSnapshot("\\\\server\\share\\", DriveType.Network, WindowsDriveReadiness.Ready),
            new WindowsDriveSnapshot(string.Empty, DriveType.Unknown, WindowsDriveReadiness.NotReady),
            new WindowsDriveSnapshot("c:\\", DriveType.Fixed, WindowsDriveReadiness.Ready),
        ]);

        DriveCatalogSucceeded succeeded = await ListAsync(enumerator);

        Assert.HasCount(1, succeeded.Drives);
        Assert.AreEqual("C:\\", succeeded.Drives[0].Root.CanonicalText);
        Assert.AreEqual(3, succeeded.UnrepresentableRootCount);
    }

    /// <summary>Proves listing failures normalize to their closed reasons.</summary>
    [TestMethod]
    [DataRow("io")]
    [DataRow("access")]
    public async Task ListAsyncWhenEnumerationFailsReturnsClosedFailureAsync(string failureCase)
    {
        ScriptedDriveEnumerator enumerator = new([])
        {
            EnumerateFailure = failureCase == "io" ? new IOException() : new UnauthorizedAccessException(),
        };
        ManualIoScheduler scheduler = new();
        WindowsDriveCatalog catalog = new(new WindowsLocalIoExecutionBoundary(scheduler), enumerator);

        Task<DriveCatalogOutcome> listing = catalog.ListAsync(CancellationToken.None);
        scheduler.ExecuteAll();

        DriveCatalogFailed failed = Assert.IsInstanceOfType<DriveCatalogFailed>(await listing);

        Assert.AreSame(
            failureCase == "io" ? DriveCatalogFailureKind.ProviderUnavailable : DriveCatalogFailureKind.AccessDenied,
            failed.Failure);
    }

    /// <summary>Proves the catalog rejects absent collaborators.</summary>
    [TestMethod]
    public void ConstructorWhenCollaboratorIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsDriveCatalog(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsDriveCatalog(null!, new ScriptedDriveEnumerator([])));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsDriveCatalog(new WindowsLocalIoExecutionBoundary(), null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsDriveSnapshot(null!, DriveType.Fixed, WindowsDriveReadiness.Ready));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsDriveSnapshot("C:\\", DriveType.Fixed, null!));
    }

    private static async Task<DriveCatalogSucceeded> ListAsync(ScriptedDriveEnumerator enumerator)
    {
        ManualIoScheduler scheduler = new();
        WindowsDriveCatalog catalog = new(new WindowsLocalIoExecutionBoundary(scheduler), enumerator);
        Task<DriveCatalogOutcome> listing = catalog.ListAsync(CancellationToken.None);
        scheduler.ExecuteAll();
        return Assert.IsInstanceOfType<DriveCatalogSucceeded>(await listing);
    }

    private sealed class ScriptedDriveEnumerator : IWindowsDriveEnumerator
    {
        private readonly IReadOnlyList<WindowsDriveSnapshot> _drives;

        internal ScriptedDriveEnumerator(IReadOnlyList<WindowsDriveSnapshot> drives)
        {
            _drives = drives;
        }

        internal Dictionary<string, string> Labels { get; } = [];

        internal List<string> LabelReads { get; } = [];

        internal Exception? EnumerateFailure { get; init; }

        internal Exception? LabelFailure { get; set; }

        internal int EnumerateCount { get; private set; }

        IReadOnlyList<WindowsDriveSnapshot> IWindowsDriveEnumerator.Enumerate()
        {
            EnumerateCount++;
            return EnumerateFailure is null ? _drives : throw EnumerateFailure;
        }

        string IWindowsDriveEnumerator.ReadVolumeLabel(string rootName)
        {
            LabelReads.Add(rootName);
            return LabelFailure is null ? Labels.GetValueOrDefault(rootName, string.Empty) : throw LabelFailure;
        }
    }

    private sealed class ManualIoScheduler : IWindowsLocalIoScheduler
    {
        private readonly Queue<Action> _pending = new();

        internal int PendingCount => _pending.Count;

        internal void ExecuteAll()
        {
            while (_pending.Count > 0)
            {
                _pending.Dequeue()();
            }
        }

        public Task<TResult> ScheduleAsync<TResult>(Func<TResult> operation)
        {
            TaskCompletionSource<TResult> completion = new();
            _pending.Enqueue(() => completion.SetResult(operation()));
            return completion.Task;
        }
    }
}
