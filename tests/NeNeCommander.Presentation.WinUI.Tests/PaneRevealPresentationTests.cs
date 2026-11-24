using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves reveal wording follows the handoff purpose while normal activation keeps its statuses.</summary>
[TestClass]
public sealed class PaneRevealPresentationTests
{
    /// <summary>Proves every reveal outcome uses reveal-specific resources for each admitted provider shape.</summary>
    [TestMethod]
    [DataRow("C:\\root")]
    [DataRow("\\\\server\\share\\folder")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home")]
    public async Task PresentWhenRevealChangesActivityUsesPurposeQualifiedStatusAsync(string location)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            PaneListingTestContext.CreateListing(location, ["a.txt"], DirectoryListingCompleteness.Complete, 0), null));
        ScriptedFileLauncher launcher = new();
        TaskCompletionSource<FileLaunchOutcome> release = launcher.EnqueuePending();
        launcher.Enqueue(FileLaunchOutcome.Cancelled());
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.NotFound));
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.AccessDenied));
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.AssociationUnavailable));
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.ShellRejected));
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.ProviderUnavailable));
        PaneSession session = PaneListingTestContext.CreateSession(port, launcher);
        _ = await session.NavigateAsync(PaneListingTestContext.ParsePath(location), CancellationToken.None);

        Task<PaneSnapshot> pending = session.HandleAsync(UserIntent.RevealFocused, CancellationToken.None);
        Assert.AreSame(PaneStatus.Revealing, PaneListingTestContext.Present(session.Current).Status);
        release.SetResult(FileLaunchOutcome.Accepted());
        _ = await pending;

        PaneStatus[] expected = [PaneStatus.RevealCancelled, PaneStatus.RevealNotFound,
            PaneStatus.RevealAccessDenied, PaneStatus.RevealUnavailable,
            PaneStatus.RevealUnavailable, PaneStatus.RevealUnavailable];
        foreach (PaneStatus status in expected)
        {
            Assert.AreSame(status, PaneListingTestContext.Present(
                await session.HandleAsync(UserIntent.RevealFocused, CancellationToken.None)).Status);
        }
    }
}
