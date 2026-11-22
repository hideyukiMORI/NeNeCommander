using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves opposite navigation is admitted only from an idle listed source and captures its visible location.</summary>
[TestClass]
public sealed class OtherPaneNavigationAdmissionTests
{
    /// <summary>Proves an absent source cannot borrow the opposite pane's location.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSourceIsAbsentDoesNothingAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing target = DualPaneSessionTestContext.Listing(@"D:\target");
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(target, null));
        DualPaneSnapshot before = await fixture.Panes.NavigateAsync(PaneSide.Right, target.Location, CancellationToken.None);

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        Assert.IsFalse(before.CanOpenCurrentLocationInOtherPane);
        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(before.Right, after.Right);
        Assert.IsEmpty(fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Proves neither pane's existing read is replaced or cancelled at command admission.</summary>
    [TestMethod]
    public Task HandleAsyncWhenEitherPaneIsLoadingRefusesWithoutSupersedingLeftAsync()
    {
        return AssertWhenEitherPaneIsLoadingRefusesWithoutSupersedingAsync(PaneSide.Left);
    }

    /// <summary>Proves the same boundary for the other explicitly named case.</summary>
    [TestMethod]
    public Task HandleAsyncWhenEitherPaneIsLoadingRefusesWithoutSupersedingRightAsync()
    {
        return AssertWhenEitherPaneIsLoadingRefusesWithoutSupersedingAsync(PaneSide.Right);
    }

    private static async Task AssertWhenEitherPaneIsLoadingRefusesWithoutSupersedingAsync(PaneSide loadingSide)
    {
        bool sourceLoading = loadingSide == PaneSide.Left;
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        ScriptedDirectoryReadPort port = sourceLoading ? fixture.Left : fixture.Right;
        PaneSide side = sourceLoading ? PaneSide.Left : PaneSide.Right;
        TaskCompletionSource<DirectoryReadOutcome> provider = port.EnqueuePending();
        Task<DualPaneSnapshot> navigation = fixture.Panes.NavigateAsync(side, DualPaneSessionTestContext.ParsePath(@"C:\pending"), CancellationToken.None);
        DualPaneSnapshot before = fixture.Panes.Current;

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        Assert.IsFalse(before.CanOpenCurrentLocationInOtherPane);
        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(before.Right, after.Right);
        Assert.HasCount(2, port.Requests);
        Assert.HasCount(1, sourceLoading ? fixture.Right.Requests : fixture.Left.Requests);
        Assert.IsFalse(port.Tokens[1].IsCancellationRequested);
        provider.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await navigation;
    }

    /// <summary>Proves either side's in-flight Shell handoff refuses opposite navigation without another launch.</summary>
    [TestMethod]
    public Task HandleAsyncWhenEitherPaneIsLaunchingRefusesWithoutRetargetingLeftAsync()
    {
        return AssertWhenEitherPaneIsLaunchingRefusesWithoutRetargetingAsync(PaneSide.Left);
    }

    /// <summary>Proves the same boundary for the other explicitly named case.</summary>
    [TestMethod]
    public Task HandleAsyncWhenEitherPaneIsLaunchingRefusesWithoutRetargetingRightAsync()
    {
        return AssertWhenEitherPaneIsLaunchingRefusesWithoutRetargetingAsync(PaneSide.Right);
    }

    private static async Task AssertWhenEitherPaneIsLaunchingRefusesWithoutRetargetingAsync(PaneSide launchingSide)
    {
        bool sourceLaunching = launchingSide == PaneSide.Left;
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedFileLauncher leftLauncher = new();
        ScriptedFileLauncher rightLauncher = new();
        ScriptedFileLauncher launcher = sourceLaunching ? leftLauncher : rightLauncher;
        TaskCompletionSource<FileLaunchOutcome> provider = launcher.EnqueuePending();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        _ = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent()))
            .CreateSession(leftLauncher, rightLauncher, out DualPaneSession panes);
        DirectoryListing source = DualPaneSessionTestContext.Listing(@"C:\source", ("source.txt", DirectoryEntryKind.File));
        DirectoryListing target = DualPaneSessionTestContext.Listing(@"D:\target", ("target.txt", DirectoryEntryKind.File));
        left.Enqueue(DirectoryReadOutcome.Succeeded(source, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(target, null));
        _ = await panes.NavigateAsync(PaneSide.Left, source.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, target.Location, CancellationToken.None);
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();
        if (!sourceLaunching)
        {
            _ = await panes.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        }
        Task<DualPaneSnapshot> launching = panes.HandleAsync(UserIntent.OpenFocused, observer, CancellationToken.None);
        if (!sourceLaunching)
        {
            _ = await panes.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        }
        DualPaneSnapshot before = panes.Current;

        DualPaneSnapshot after = await panes.HandleAsync(UserIntent.OpenCurrentLocationInOtherPane, observer, CancellationToken.None);

        Assert.IsFalse(before.CanOpenCurrentLocationInOtherPane);
        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(before.Right, after.Right);
        Assert.HasCount(1, left.Requests);
        Assert.HasCount(1, right.Requests);
        Assert.HasCount(1, launcher.Targets);
        Assert.IsEmpty(sourceLaunching ? rightLauncher.Targets : leftLauncher.Targets);
        Assert.IsFalse(launcher.CancellationTokens[0].IsCancellationRequested);
        provider.SetResult(FileLaunchOutcome.Accepted());
        _ = await launching;
    }

    /// <summary>Proves a retained source opens its displayed successful location, not its failed or abandoned target.</summary>
    [TestMethod]
    [DataRow("failed")]
    [DataRow("cancelled")]
    [DataRow("abandoned")]
    public async Task HandleAsyncWhenSourceHasRetainedContentUsesVisibleLocationAsync(string activity)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        _ = await HandleAsync(fixture, UserIntent.ToggleSelection);
        TaskCompletionSource<DirectoryReadOutcome> provider = fixture.Left.EnqueuePending();
        Task<DualPaneSnapshot> navigation = fixture.Panes.NavigateAsync(PaneSide.Left, DualPaneSessionTestContext.ParsePath(@"C:\not-visible"), CancellationToken.None);
        if (activity == "abandoned")
        {
            _ = await HandleAsync(fixture, UserIntent.Escape);
        }
        provider.SetResult(activity == "failed"
            ? DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : DirectoryReadOutcome.Cancelled());
        _ = await navigation;
        DualPaneSnapshot before = fixture.Panes.Current;
        DirectoryListing fresh = DualPaneSessionTestContext.Listing(@"C:\source");
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(fresh, null));

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        Assert.IsTrue(before.CanOpenCurrentLocationInOtherPane);
        Assert.AreSame(before.Left, after.Left);
        Assert.HasCount(1, Assert.IsInstanceOfType<PaneContentListed>(after.Left.Content).State.Selection);
        Assert.AreEqual(@"C:\source", fixture.Right.Requests[1].Location.CanonicalText);
        Assert.AreSame(fresh, Assert.IsInstanceOfType<PaneContentListed>(after.Right.Content).Listing);
    }

    /// <summary>Proves an absent failed destination remains eligible for the source location.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDestinationPreviouslyFailedCanNavigateItAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing source = DualPaneSessionTestContext.Listing(@"C:\source");
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(source, null));
        _ = await fixture.Panes.NavigateAsync(PaneSide.Left, source.Location, CancellationToken.None);
        fixture.Right.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
        _ = await fixture.Panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath(@"D:\unavailable"), CancellationToken.None);
        DualPaneSnapshot before = fixture.Panes.Current;
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(source, null));

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        Assert.IsTrue(before.CanOpenCurrentLocationInOtherPane);
        Assert.AreSame(PaneContent.Absent, before.Right.Content);
        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(source, Assert.IsInstanceOfType<PaneContentListed>(after.Right.Content).Listing);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    private static Task ListAsync(DualPaneSessionTestContext fixture)
    {
        return fixture.ListBothAsync(
            DualPaneSessionTestContext.Listing(@"C:\source", ("source.txt", DirectoryEntryKind.File)),
            DualPaneSessionTestContext.Listing(@"D:\target", ("target.txt", DirectoryEntryKind.File)));
    }

    private static Task<DualPaneSnapshot> HandleAsync(DualPaneSessionTestContext fixture, UserIntent intent)
    {
        return fixture.Panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
    }
}
