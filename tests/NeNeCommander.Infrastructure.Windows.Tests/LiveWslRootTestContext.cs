using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Shares the existing deterministic setup and assertions for LiveWslTestRootTests.</summary>
internal static class LiveWslRootTestContext
{
    internal const string DistributionName = "Ubuntu";
    internal const string ConfiguredName = "NeNeCommander-Live-Proof";
    internal const string RunName = "NeNeCommander-Live-Run";
    internal const string MarkerName = ".nene-commander-owner";
    internal const uint DirectoryAttribute = 0x00000010;

    internal static TestOwnedTemporaryRoot CreateHost()
    {
        TestOwnedTemporaryRoot host = CreateHostWithoutConfiguredRoot();
        _ = host.CreateDirectory(ConfiguredRelative(string.Empty));
        return host;
    }

    internal static TestOwnedTemporaryRoot CreateHostWithoutConfiguredRoot()
    {
        TestOwnedTemporaryRoot host = TestOwnedTemporaryRoot.Create();
        _ = host.CreateDirectory("distribution");
        _ = host.CreateDirectory("distribution\\tmp");
        return host;
    }

    internal static LiveWslRootOpenOutcome Open(TestOwnedTemporaryRoot host)
    {
        return Open(host, link => File.Delete(Resolve(host, link)));
    }

    internal static LiveWslRootOpenOutcome Open(TestOwnedTemporaryRoot host, Action<WslPath> unlinkEntry)
    {
        // The deterministic root injects the unguarded NTFS handle-facts reader through the
        // existing WindowsWslFileSystem seam, exactly as WindowsWslFileSystemTests do. It has no
        // distribution, so its link fixture is an NTFS symbolic link created and removed through
        // the same seams the live owner fills with `ln -s` and `unlink`.
        string Resolved(WslPath path)
        {
            return Resolve(host, path);
        }

        void CreateLink(WslPath target, WslPath link)
        {
            _ = File.CreateSymbolicLink(Resolved(link), Resolved(target));
        }

        return LiveWslTestRoot.Open(
            Admission(ConfiguredWslText()),
            [WslRoot(DistributionName)],
            new WindowsWslFileSystem(Resolved, ReadProviderShapedFacts),
            Resolved,
            CreateLink,
            unlinkEntry);
    }

    /// <summary>
    /// Reads the real NTFS handle facts and presents the one field where NTFS and 9P disagree in
    /// the 9P shape. ADR-0049 measured that a 9P directory always reports length zero, so the
    /// token treats a nonzero directory length as a provider anomaly and fails closed. NTFS
    /// instead reports the size of the <c>$I30</c> index allocation once a directory's index stops
    /// fitting in its MFT record, which depends on the entry count and the cluster size of the
    /// agent's volume. Only that field is normalized; inode, attributes, reparse tag, link count,
    /// and both timestamps stay exactly as the handle reported them.
    /// </summary>
    /// <param name="path">Existing NTFS path of the entry to read.</param>
    /// <returns>The facts snapshot in the shape the WSL provider would report.</returns>
    internal static WslHandleFacts ReadProviderShapedFacts(string path)
    {
        WslHandleFacts facts = WindowsFileIdentifier.ReadHandleFacts(path);
        return (facts.Attributes & DirectoryAttribute) == 0 || facts.EndOfFile == 0
            ? facts
            : new WslHandleFacts(facts.Inode, facts.Attributes, facts.ReparseTag, new WslRewriteFacts(0, facts.NumberOfLinks, facts.LastWriteFileTimeUtc, facts.ChangeFileTimeUtc));
    }

    internal static WindowsWslFileSystem RejectingFileSystem()
    {
        return new WindowsWslFileSystem(RejectResolution, WindowsFileIdentifier.ReadHandleFacts);
    }

    internal static LiveWslRootAdmission Admission(string? configuredRoot)
    {
        return LiveWslRootAdmission.Create(
            configuredRoot,
            LiveWslRootAdmission.HomeFactAccepted,
            LiveWslRootAdmission.MountFactAccepted);
    }

    internal static string Resolve(TestOwnedTemporaryRoot host, WslPath path)
    {
        // The harness addresses the host exactly as the live run addresses the distribution: through
        // the production resolver's extended-length text (ADR-0060).
        return OwnedWslNamespace.ResolveDistribution(host.ResolveVerbatim("distribution"), path);
    }

    internal static void RejectLinkCreation(WslPath target, WslPath link)
    {
        throw new AssertFailedException("Unsafe input reached link creation: " + link.LinuxPath.Length);
    }

    internal static void RejectUnlink(WslPath link)
    {
        throw new AssertFailedException("Unsafe input reached unlink: " + link.LinuxPath.Length);
    }

    internal static string RejectResolution(WslPath path)
    {
        throw new AssertFailedException("Unsafe input reached filesystem resolution: " + path.LinuxPath.Length);
    }

    internal static WslPath WslRoot(string distribution)
    {
        return Assert.IsInstanceOfType<WslPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse("\\\\wsl.localhost\\" + distribution + "\\")).Path);
    }

    internal static string ConfiguredWslText()
    {
        return "\\\\wsl.localhost\\" + DistributionName + "\\tmp\\" + ConfiguredName;
    }

    internal static string ConfiguredRelative(string child)
    {
        string root = "distribution\\tmp\\" + ConfiguredName;
        return child.Length == 0 ? root : root + "\\" + child;
    }
}
