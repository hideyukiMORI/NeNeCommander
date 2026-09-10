using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        LiveWslRootOpenOutcome outcome = await LiveWslTestRoot.OpenAsync(admission, CancellationToken.None);
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

    internal static void RequireCleanup(TestContext context, LiveWslTestRoot root)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(root);
        LiveWslRootCleanupOutcome cleanup = root.Cleanup();
        if (cleanup is LiveWslRootCleanupRejected rejected)
        {
            context.WriteLine(
                "LiveWsl cleanup=Rejected provider=Wsl root=redacted identity=redacted failure=" +
                rejected.Failure.GetType().Name);
            Assert.Fail("LiveWsl:CleanupRejected:" + rejected.Failure.GetType().Name);
        }
        _ = Assert.IsInstanceOfType<LiveWslRootCleanupCompleted>(cleanup);
        context.WriteLine("LiveWsl cleanup=Completed provider=Wsl root=redacted identity=redacted");
    }

    private static string? Parameter(TestContext context, string name)
    {
        return context.Properties.TryGetValue(name, out object? value) ? value as string : null;
    }
}
