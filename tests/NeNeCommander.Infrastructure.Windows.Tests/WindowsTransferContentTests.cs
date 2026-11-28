using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves bounded exact-length copying and the existing comparison owner's held-handle path.</summary>
[TestClass]
public sealed class WindowsTransferContentTests
{
    /// <summary>Proves empty and multiple-buffer copies truncate old tails and retain complete byte/hash evidence.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(65535)]
    [DataRow(65536)]
    [DataRow(65537)]
    [DataRow(131075)]
    public async Task CopyAsyncWhenBytesCrossBufferBoundariesVerifiesExactContentAsync(int length)
    {
        string source = length == 0 ? string.Empty : new string('x', length - 1) + "y";
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create(source, new string('z', length + 5), length);

        await WindowsTransferContentCopy.CopyAsync(fixture.Pair, WindowsTransferContentIo.Native, CancellationToken.None);
        FileContentEvidence evidence = await VerifiedFileContent.CompareHeldAsync(fixture.Pair, WindowsTransferContentIo.Native, CancellationToken.None);
        Assert.AreEqual(length, evidence.Size.Bytes);
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))), evidence.Hash.Value);
        Assert.AreEqual(source, fixture.Read("target"));
        Assert.IsFalse(fixture.Pair.Source.IsClosed);
        Assert.IsFalse(fixture.Pair.Target.IsClosed);
    }

    /// <summary>Proves short native reads on either side fill the bounded buffer instead of becoming a false mismatch or early completion.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenReadsReturnShortChunksFillsAndComparesEveryByteAsync()
    {
        string source = new('a', 257);
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create(source, string.Empty, source.Length);
        int readCount = 0;
        WindowsTransferContentIo io = new((handle, buffer, offset, token) =>
        {
            readCount++;
            int maximum = ReferenceEquals(handle, fixture.Pair.Source) ? 3 : 7;
            return RandomAccess.ReadAsync(handle, buffer[..Math.Min(maximum, buffer.Length)], offset, token);
        }, RandomAccess.WriteAsync, RandomAccess.SetLength, RandomAccess.FlushToDisk);

        await WindowsTransferContentCopy.CopyAsync(fixture.Pair, io, CancellationToken.None);
        FileContentEvidence evidence = await VerifiedFileContent.CompareHeldAsync(fixture.Pair, io, CancellationToken.None);
        Assert.IsGreaterThan(100, readCount);
        Assert.AreEqual(source.Length, evidence.Size.Bytes);
        Assert.AreEqual(source, fixture.Read("target"));
    }

    /// <summary>Proves later occupants of the displayed names cannot redirect already borrowed handles.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CopyAsyncWhenOriginalNamesAreReplacedUsesHeldObjectsAsync()
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create("original", string.Empty, 8);
        File.Move(fixture.SourcePath, fixture.Root.Resolve("held-source"));
        File.Move(fixture.TargetPath, fixture.Root.Resolve("held-target"));
        _ = fixture.Root.WriteFile("source", "foreign-source");
        _ = fixture.Root.WriteFile("target", "foreign-target");

        await WindowsTransferContentCopy.CopyAsync(fixture.Pair, WindowsTransferContentIo.Native, CancellationToken.None);
        _ = await VerifiedFileContent.CompareHeldAsync(fixture.Pair, WindowsTransferContentIo.Native, CancellationToken.None);
        Assert.AreEqual("original", fixture.Read("held-target"));
        Assert.AreEqual("foreign-source", File.ReadAllText(fixture.SourcePath));
        Assert.AreEqual("foreign-target", fixture.Read("target"));
    }

    /// <summary>Proves the production parent-relative synchronous handles work with bounded asynchronous I/O without reopening originals.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenHandlesComeFromNativeOwnersVerifiesOwnedArtifactAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string content = new string('x', 65536) + "tail";
        WindowsLocalPath source = Parse(root.WriteFile("source", content));
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor", "original-target"));
        using WindowsTransferPathLease sourceLease = WindowsTransferPathLease.Open(source, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferPathLease parent = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using WindowsTransferArtifactOwner artifact = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.OriginalBackup,
            Parse(root.Resolve("artifact")), parent, WindowsTransferArtifactQueries.Native);
        artifact.CreateNative(security);
        string objectIdentity = WindowsFileIdentifier.DescribeHandle(artifact.Handle);
        WindowsTransferContentPair pair = WindowsTransferContentPair.Create(sourceLease.Handle, artifact.Handle, (KnownEntrySize)EntrySize.Create(content.Length));

        await WindowsTransferContentCopy.CopyAsync(pair, WindowsTransferContentIo.Native, CancellationToken.None);
        FileContentEvidence evidence = await VerifiedFileContent.CompareHeldAsync(pair, WindowsTransferContentIo.Native, CancellationToken.None);
        artifact.ObserveBeforeTargetMutation();
        sourceLease.Revalidate();
        parent.Revalidate();
        Assert.AreEqual(content.Length, evidence.Size.Bytes);
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))), evidence.Hash.Value);
        Assert.AreEqual(objectIdentity, WindowsFileIdentifier.DescribeHandle(artifact.Handle));
        Assert.AreEqual("original-target", File.ReadAllText(anchor.CanonicalText));
    }

    /// <summary>Proves both EOFs and the final differing byte are checked after the expected range.</summary>
    [TestMethod]
    [DataRow("abc", "abc", 4)]
    [DataRow("abcd", "abcd", 3)]
    [DataRow("abc", "abcd", 3)]
    [DataRow("abcd", "abc", 4)]
    [DataRow("abc", "abX", 3)]
    [DataRow("", "x", 0)]
    [DataRow("x", "", 0)]
    public async Task CompareHeldAsyncWhenBytesOrEitherLengthDifferRefusesEvidenceAsync(string source, string target, long expected)
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create(source, target, expected);

        _ = await Assert.ThrowsExactlyAsync<IOException>(() => VerifiedFileContent.CompareHeldAsync(fixture.Pair, WindowsTransferContentIo.Native, CancellationToken.None));
    }

    /// <summary>Proves copying a stale admitted length fails rather than treating early EOF or extra source bytes as success.</summary>
    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    public async Task CopyAsyncWhenSourceLengthDoesNotMatchAdmissionRefusesCompletionAsync(long expected)
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create("abc", "old-target", expected);

        _ = await Assert.ThrowsExactlyAsync<IOException>(() => WindowsTransferContentCopy.CopyAsync(fixture.Pair, WindowsTransferContentIo.Native, CancellationToken.None));
        Assert.AreEqual("abc", File.ReadAllText(fixture.SourcePath));
    }
    private static WindowsLocalPath Parse(string path)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path);
    }
}
