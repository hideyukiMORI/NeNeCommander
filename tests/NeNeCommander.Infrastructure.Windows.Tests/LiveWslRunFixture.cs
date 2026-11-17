using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Wsl;
using NeNeCommander.Infrastructure.Windows.FileOperations;
using NeNeCommander.Infrastructure.Windows.Paths;
using NeNeCommander.Infrastructure.Windows.Wsl;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Opens and closes the one opt-in live WSL run root for every live cell. The launcher passes the
/// configured root and its closed admission facts through ephemeral runsettings; ADR-0049 keeps
/// every identity inside the C# owner.
/// </summary>
internal static class LiveWslRunFixture
{
    private const string RootParameterName = "NENE_COMMANDER_WSL_TEST_ROOT";
    private const string HomeFactParameterName = "NENE_COMMANDER_WSL_HOME_FACT";
    private const string MountFactParameterName = "NENE_COMMANDER_WSL_MOUNT_FACT";

    internal static async Task<LiveWslTestRoot> OpenAsync(TestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        LiveWslRootAdmission admission = LiveWslRootAdmission.Create(
            Parameter(context, RootParameterName),
            Parameter(context, HomeFactParameterName),
            Parameter(context, MountFactParameterName));
        LiveWslRootOpenOutcome outcome = await OpenProductionRootAsync(admission, CancellationToken.None);
        if (outcome is LiveWslRootOpenRejected { Failure: var failure })
        {
            if (failure == LiveWslRootFailureKind.Unexecuted)
            {
                Assert.Inconclusive("LiveWsl:Unexecuted:RootParameterAbsent");
            }
            Assert.Fail("LiveWsl:RootRejected:" + failure.GetType().Name);
        }
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(outcome).Root;
        context.WriteLine("LiveWsl setup=Opened provider=Wsl root=redacted identity=redacted");
        return root;
    }

    internal static void RequireEffectBoundary(LiveWslTestRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _ = Assert.IsInstanceOfType<LiveWslRootCheckAccepted>(root.VerifyForEffect());
    }

    /// <summary>
    /// Closes the run root and records the redacted outcome. This runs from a cell's finally block
    /// and never asserts, so a failing cell reports its own cause instead of a cleanup assertion
    /// raised while that cause is still in flight.
    /// </summary>
    /// <param name="context">Test context that receives the redacted record.</param>
    /// <param name="root">Run root to close.</param>
    /// <returns>The cleanup outcome for the cell to require once its body succeeded.</returns>
    internal static LiveWslRootCleanupOutcome Close(TestContext context, LiveWslTestRoot root)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(root);
        LiveWslRootCleanupOutcome cleanup = root.Cleanup();
        context.WriteLine(
            "LiveWsl cleanup=" + cleanup.GetType().Name +
            " provider=Wsl root=redacted identity=redacted" +
            (cleanup is LiveWslRootCleanupRejected rejected
                ? " failure=" + rejected.Failure.GetType().Name
                : string.Empty));
        return cleanup;
    }

    internal static void RequireCleanup(LiveWslRootCleanupOutcome cleanup)
    {
        if (cleanup is LiveWslRootCleanupRejected rejected)
        {
            Assert.Fail("LiveWsl:CleanupRejected:" + rejected.Failure.GetType().Name);
        }
        _ = Assert.IsInstanceOfType<LiveWslRootCleanupCompleted>(cleanup);
    }

    private static async Task<LiveWslRootOpenOutcome> OpenProductionRootAsync(
        LiveWslRootAdmission admission,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(admission);
        if (admission.ConfiguredRoot is null)
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.Unexecuted);
        }
        if (!admission.IsComplete)
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.InvalidConfiguration);
        }

        WslDistributionCatalogOutcome discovery = await new WslDistributionCatalog().DiscoverAsync(cancellationToken);

        // The live run uses the production WSL file system exactly as the composition root does:
        // the extended-length namespace mapping of ADR-0060 and the ADR-0049 9P-guarded
        // handle-facts reader.
        return discovery is WslDistributionCatalogSucceeded succeeded
            ? LiveWslTestRoot.Open(
                admission,
                succeeded.Roots,
                LiveWslRootMechanism.Create(
                    new WindowsWslFileSystem(),
                    WslWin32PathText.Resolve,
                    LiveWslLinkFixture.Create,
                    LiveWslLinkFixture.Unlink))
            : new LiveWslRootOpenRejected(LiveWslRootFailureKind.DistributionUnavailable);
    }

    private static string? Parameter(TestContext context, string name)
    {
        return context.Properties.TryGetValue(name, out object? value) ? value as string : null;
    }
}
