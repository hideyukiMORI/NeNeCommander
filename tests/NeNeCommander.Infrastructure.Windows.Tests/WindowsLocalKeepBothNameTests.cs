using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the visible KeepBoth candidate name keeps a directory name whole.</summary>
[TestClass]
public sealed class WindowsLocalKeepBothNameTests
{
    /// <summary>Proves a directory keeps its dotted name whole and receives the suffix at the end.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenDottedDirectoryConflictsSuffixesWholeName()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath source = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("tree.v2"));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("destination"));
        _ = root.CreateDirectory("destination\\tree.v2");
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter(new DeterministicIdentifierSource()));
        CopyRequest request = (CopyRequest)((FileOperationRequestAccepted)CopyRequest.Create([source], destination)).Request;

        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            request, IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.IsNotNull(awaiting.Conflicts);
        Assert.AreEqual(root.Resolve("destination\\tree.v2 (2)"),
            awaiting.Conflicts.Conflicts[0].KeepBothCandidate.CanonicalText, ignoreCase: true);
    }
}
