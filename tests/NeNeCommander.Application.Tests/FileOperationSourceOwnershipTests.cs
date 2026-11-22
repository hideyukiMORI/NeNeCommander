using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves accepted operation sources remain frozen through public reads and provider callbacks.</summary>
[TestClass]
public sealed class FileOperationSourceOwnershipTests
{
    /// <summary>Proves every accepted request and its record copy retain their published source identities.</summary>
    [TestMethod]
    [DataRow("Move")]
    [DataRow("Copy")]
    [DataRow("Delete")]
    [DataRow("CreateDirectory")]
    [DataRow("CreateTextFile")]
    [DataRow("Rename")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public void CreateWhenPublishedSourcesAreInspectedRetainsAcceptedSources(string kind)
    {
        FileOperationRequest request = CreateRequest(kind);
        FileOperationRequest copied = request with { };
        FileSystemPath[] expected = [.. request.Sources];

        TryClearThroughSyncRoot(copied.Sources);

        CollectionAssert.AreEqual(expected, request.Sources.ToArray());
        CollectionAssert.AreEqual(expected, copied.Sources.ToArray());
        Assert.AreSame(request.Sources, copied.Sources);
        Assert.AreEqual(request, copied);
        Assert.AreEqual(request.GetHashCode(), copied.GetHashCode());
    }

    /// <summary>Proves generic reads and copies preserve order while mutation remains unavailable.</summary>
    [TestMethod]
    [DataRow("Move")]
    [DataRow("Copy")]
    [DataRow("Delete")]
    [DataRow("CreateDirectory")]
    [DataRow("CreateTextFile")]
    [DataRow("Rename")]
    public void SourcesWhenReadOrCopiedPreserveOrderAndRejectMutation(string kind)
    {
        FileOperationRequest request = CreateRequest(kind);
        IReadOnlyList<FileSystemPath> sources = request.Sources;
        FileSystemPath[] expected = [.. sources];
        ICollection<FileSystemPath> collection = Assert.IsInstanceOfType<ICollection<FileSystemPath>>(sources);
        FileSystemPath replacement = GatewayTestContext.ParsePath("C:\\replacement");
        FileSystemPath[] copied = new FileSystemPath[sources.Count + 1];

        collection.CopyTo(copied, 1);
        CollectionAssert.AreEqual(expected, copied.Skip(1).ToArray());
        copied[1] = replacement;
        _ = Assert.ThrowsExactly<NotSupportedException>(() => collection.Add(replacement));
        _ = Assert.ThrowsExactly<NotSupportedException>(collection.Clear);
        _ = Assert.ThrowsExactly<NotSupportedException>(() => collection.Remove(expected[0]));

        Assert.IsTrue(collection.IsReadOnly);
        Assert.IsTrue(collection.Contains(expected[0]));
        Assert.IsFalse(collection.Contains(replacement));
        CollectionAssert.AreEqual(expected, sources.ToArray());
        AssertNonGenericEnumeration(sources);
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sources[-1]);
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sources[sources.Count]);
    }

    /// <summary>Proves caller storage never changes the accepted source order or identity references.</summary>
    [TestMethod]
    [DataRow("Move")]
    [DataRow("Copy")]
    [DataRow("Delete")]
    public void CreateWhenCallerReplacesSourceRetainsOriginalOrderedSources(string kind)
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        FileSystemPath[] input = [first, second];
        FileOperationRequest request = CreateBatch(kind, input);

        input[0] = second;
        input[1] = first;

        Assert.HasCount(2, request.Sources);
        Assert.AreSame(first, request.Sources[0]);
        Assert.AreSame(second, request.Sources[1]);
    }

    /// <summary>Proves a callback after a completed delete cannot detach the effect from its outcome or progress.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task ExecuteAsyncWhenPublishedSourcesAreInspectedAfterDeletionRetainsEffectAndProgress()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        DeleteRequest request = GatewayTestContext.CreateDelete([path], null);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterDeletion,
            () => TryClearThroughSyncRoot(request.Sources));
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.Recycle));
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, progress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Recycled, outcome.Effects[0].Kind);
        Assert.HasCount(2, port.Calls);
        Assert.AreEqual("Recycle:" + path.CanonicalText, port.Calls[1]);
        Assert.HasCount(1, progress.Reports);
        Assert.AreEqual(1, progress.Reports[0].Completed);
        Assert.AreEqual(1, progress.Reports[0].Total);
        Assert.AreSame(path, request.Sources[0]);
    }

    private static void AssertNonGenericEnumeration(IReadOnlyList<FileSystemPath> sources)
    {
        IEnumerable enumerable = sources;
        IEnumerator iterator = enumerable.GetEnumerator();
        using IDisposable lifetime = Assert.IsInstanceOfType<IDisposable>(iterator);
        int index = 0;
        while (iterator.MoveNext())
        {
            Assert.AreSame(sources[index], iterator.Current);
            index++;
        }
        Assert.AreEqual(sources.Count, index);
    }
    private static FileOperationRequest CreateRequest(string kind)
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source");
        return kind switch
        {
            "CreateDirectory" => GatewayTestContext.CreateCreateDirectory(source, "child"),
            "CreateTextFile" => Assert.IsInstanceOfType<FileOperationRequestAccepted>(CreateTextFileRequest.Create(source, "child")).Request,
            "Rename" => GatewayTestContext.CreateRename(source, "renamed"),
            _ => CreateBatch(kind, [source, GatewayTestContext.ParsePath("C:\\second")]),
        };
    }

    private static FileOperationRequest CreateBatch(string kind, FileSystemPath[] sources)
    {
        return kind switch
        {
            "Move" => GatewayTestContext.CreateMove(sources),
            "Copy" => GatewayTestContext.CreateCopy(sources),
            "Delete" => GatewayTestContext.CreateDelete(sources, null),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    private static void TryClearThroughSyncRoot(IReadOnlyList<FileSystemPath> sources)
    {
        if (sources is ICollection collection &&
            collection.SyncRoot is ICollection<FileSystemPath> mutable && !mutable.IsReadOnly)
        {
            mutable.Clear();
        }
    }
}
