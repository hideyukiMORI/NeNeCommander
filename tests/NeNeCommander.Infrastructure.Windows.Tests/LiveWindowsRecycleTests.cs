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

/// <summary>Exercises real Shell recycling only for explicitly opted-in, owned fixtures.</summary>
[TestClass]
[DoNotParallelize]
public sealed class LiveWindowsRecycleTests
{
    /// <summary>Gets the explicit root passed by the live launcher.</summary>
    public TestContext? TestContext { get; set; }

    /// <summary>Recycles a file, proves its returned identity and restores its exact bytes.</summary>
    [TestMethod]
    [TestCategory("LiveRecycle")]
    public async Task RecycleWhenLiveFileIsOwnedReturnsReceiptAndRestoresAsync()
    {
        TestOwnedTemporaryRoot root = LiveWindowsRecycleFixture.OpenRoot(RequireContext());
        WindowsLocalPath source = WindowsRecycleBinTests.Path(root.WriteFile("source.txt", "owned recycle content"));
        LiveWindowsRecycleFixture fixture = new(root, source);
        _ = await new WindowsShellIoScheduler().ScheduleAsync(() => RecycleAndRestore(fixture));
        Assert.AreEqual("owned recycle content", File.ReadAllText(source.CanonicalText));
        fixture.Cleanup();
    }

    /// <summary>A directory tree is restored from its single returned receipt.</summary>
    [TestMethod]
    [TestCategory("LiveRecycle")]
    public async Task RecycleWhenLiveDirectoryIsOwnedRestoresItsTreeAsync()
    {
        TestOwnedTemporaryRoot root = LiveWindowsRecycleFixture.OpenRoot(RequireContext());
        WindowsLocalPath source = WindowsRecycleBinTests.Path(root.CreateDirectory("tree"));
        _ = root.CreateDirectory("tree\\nested");
        _ = root.WriteFile("tree\\nested\\payload.txt", "owned nested content");
        LiveWindowsRecycleFixture fixture = new(root, source);
        _ = await new WindowsShellIoScheduler().ScheduleAsync(() => RecycleAndRestore(fixture));
        Assert.AreEqual("owned nested content", File.ReadAllText(root.Resolve("tree\\nested\\payload.txt")));
        fixture.Cleanup();
    }

    /// <summary>The real Shell cannot permanently delete even an owned fixture through this sink.</summary>
    [TestMethod]
    [TestCategory("LiveRecycle")]
    public async Task PreDeleteWhenLiveShellProposesPermanentDeleteVetoesItAsync()
    {
        TestOwnedTemporaryRoot root = LiveWindowsRecycleFixture.OpenRoot(RequireContext());
        LiveWindowsRecycleFixture fixture = new(root, WindowsRecycleBinTests.Path(root.WriteFile("source.txt", "keep")));
        _ = await new WindowsShellIoScheduler().ScheduleAsync(() => RequireNativeVeto(fixture));
        Assert.AreEqual("keep", File.ReadAllText(fixture.Source.Path.CanonicalText));
        fixture.Cleanup();
    }

    private static ProviderStepOutcome RecycleAndRestore(LiveWindowsRecycleFixture fixture)
    {
        WindowsRecycleBin bin = new(new WindowsRecycleShell());
        Assert.AreSame(DeletionCapability.Recycle, bin.GetCapability((WindowsLocalPath)fixture.Source.Path));
        fixture.RequireRoot();
        using WindowsRecycleSession session = bin.OpenSession(fixture.Source, CancellationToken.None);
        ProviderStepOutcome outcome = session.Execute(CancellationToken.None);
        if (session.ObservedEntry is IWindowsShellEntry receipt)
        {
            fixture.Restore(receipt);
        }
        Assert.IsNull(outcome.Failure, "Post result=" + session.PostResult.ToString("X8", System.Globalization.CultureInfo.InvariantCulture) +
            " flags=" + session.PostFlags.ToString("X8", System.Globalization.CultureInfo.InvariantCulture) + " receipt=" + (session.ObservedEntry is not null));
        Assert.IsNotNull(session.RecycledEntry);
        return outcome;
    }

    private static int RequireNativeVeto(LiveWindowsRecycleFixture fixture)
    {
        WindowsRecycleShell shell = new();
        using WindowsShellEntry entry = (WindowsShellEntry)shell.OpenEntry((WindowsLocalPath)fixture.Source.Path);
        using WindowsShellOperation operation = (WindowsShellOperation)shell.OpenOperation();
        WindowsRecycleProgress progress = new(fixture.Source, CancellationToken.None);
        Assert.AreEqual(0, operation.SetOperationFlags(WindowsRecycleSession.OperationFlags & ~0x00080000u));
        Assert.AreEqual(0, operation.DeleteItem(entry, progress));
        _ = operation.PerformOperations();
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, progress.Failure);
        Assert.IsNull(progress.RecycledEntry);
        return 0;
    }

    private TestContext RequireContext()
    {
        return TestContext ?? throw new InvalidOperationException("MSTest did not supply context.");
    }
}
