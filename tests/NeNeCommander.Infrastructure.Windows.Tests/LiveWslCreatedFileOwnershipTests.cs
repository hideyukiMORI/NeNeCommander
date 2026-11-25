using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Directories;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves only a matching creation receipt can adopt a produced regular empty file.</summary>
[TestClass]
public sealed class LiveWslCreatedFileOwnershipTests
{
    /// <summary>Proves the owner accepts one empty file and then safely cleans its run child.</summary>
    [TestMethod]
    public async Task AdoptCreatedFileAsyncWhenReceiptMatchesOwnsExactEmptyFile()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        WslPath target = LiveWslRootPath.Child(root.RunRoot, "new.txt");
        File.WriteAllBytes(LiveWslRootTestContext.Resolve(host, target), []);
        FileOperationOutcome receipt = await ReceiptAsync(root.RunRoot, "new.txt", "file");

        root.AdoptCreatedFile("new.txt", receipt);

        Assert.IsEmpty(root.ReadFile("new.txt"));
        _ = Assert.IsInstanceOfType<LiveWslRootCheckAccepted>(root.VerifyForEffect());
        _ = Assert.IsInstanceOfType<LiveWslRootCleanupCompleted>(root.Cleanup());
    }

    /// <summary>Proves absent or mismatched effects cannot convert foreign residue into owned data.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [DataRow("none")]
    [DataRow("wrong-path")]
    [DataRow("wrong-kind")]
    public async Task AdoptCreatedFileAsyncWhenReceiptDoesNotMatchRefusesOwnership(string scenario)
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        WslPath target = LiveWslRootPath.Child(root.RunRoot, "new.txt");
        string resolved = LiveWslRootTestContext.Resolve(host, target);
        File.WriteAllBytes(resolved, []);
        FileOperationOutcome receipt = await ReceiptAsync(root.RunRoot,
            scenario == "wrong-path" ? "other.txt" : "new.txt",
            scenario == "wrong-kind" ? "directory" : scenario);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => root.AdoptCreatedFile("new.txt", receipt));

        _ = Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(root.Cleanup());
        Assert.IsTrue(File.Exists(resolved));
    }

    /// <summary>Proves a directory, link or nonempty file cannot satisfy empty regular-file evidence.</summary>
    [TestMethod]
    [DataRow("directory")]
    [DataRow("link")]
    [DataRow("nonempty")]
    public async Task AdoptCreatedFileAsyncWhenProducedKindOrBytesDifferRefusesEvidence(string scenario)
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        _ = root.WriteFile("sentinel.txt", [7, 8]);
        WslPath target = LiveWslRootPath.Child(root.RunRoot, "new.txt");
        string resolved = LiveWslRootTestContext.Resolve(host, target);
        if (scenario == "directory")
        {
            _ = Directory.CreateDirectory(resolved);
        }
        else if (scenario == "link")
        {
            _ = File.CreateSymbolicLink(resolved, LiveWslRootTestContext.Resolve(host, root.OwnedPath("sentinel.txt")));
        }
        else
        {
            File.WriteAllBytes(resolved, [1]);
        }
        FileOperationOutcome receipt = await ReceiptAsync(root.RunRoot, "new.txt", "file");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => root.AdoptCreatedFile("new.txt", receipt));

        _ = Assert.IsInstanceOfType<LiveWslRootCleanupRejected>(root.Cleanup());
        Assert.IsTrue(File.Exists(resolved) || Directory.Exists(resolved));
        if (scenario == "nonempty")
        {
            CollectionAssert.AreEqual(new byte[] { 1 }, File.ReadAllBytes(resolved));
        }
    }
    private static async Task<FileOperationOutcome> ReceiptAsync(WslPath location, string name, string kind)
    {
        ScriptedWslFileSystem fileSystem = new();
        FileSystemPath? path = location;
        while (path is WslPath current)
        {
            fileSystem.Set(ScriptedWslFileSystem.Entry(current, "directory:" + current.LinuxPath, DirectoryEntryKind.Directory));
            path = current.Parent;
        }
        using FileOperationGateway gateway = new(WslOperationTestContext.Adapter(fileSystem));
        FileOperationRequest request = Assert.IsInstanceOfType<FileOperationRequestAccepted>(kind == "directory"
            ? CreateDirectoryRequest.Create(location, name)
            : CreateTextFileRequest.Create(location, name)).Request;
        return await gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), new CancellationToken(kind == "none"));
    }

}
