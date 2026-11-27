using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves the KeepBoth candidate name is offered up to the 255-character name limit and that a
/// conflict whose candidate would exceed the limit stops without writing anything.
/// </summary>
[TestClass]
public sealed class WindowsLocalKeepBothNameBoundaryTests
{
    private const string Suffix = " (2)";
    private const int NameLimit = 255;

    /// <summary>Proves a candidate name of exactly 255 characters is offered.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenKeepBothCandidateNameIsExactlyAtLimitOffersIt()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string name = new('n', NameLimit - Suffix.Length);

        FileOperationOutcome outcome = await CopyIntoConflictAsync(root, name);

        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.IsNotNull(outcome.Conflicts);
        string candidate = outcome.Conflicts.Conflicts[0].KeepBothCandidate.CanonicalText;
        Assert.AreEqual(root.Resolve("destination\\" + name + Suffix), candidate, ignoreCase: true);
        Assert.HasCount(NameLimit, Path.GetFileName(candidate));
    }

    /// <summary>
    /// Proves a conflict whose candidate name would be 256 characters stops with a closed failure
    /// and leaves the existing destination entry as it was.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenKeepBothCandidateNameExceedsLimitStopsWithoutWriting()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string name = new('n', NameLimit - Suffix.Length + 1);

        FileOperationOutcome outcome = await CopyIntoConflictAsync(root, name);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsNull(outcome.Conflicts);
        Assert.IsEmpty(outcome.Effects);
        Assert.AreEqual("existing", File.ReadAllText(@"\\?\" + root.Resolve("destination\\" + name)));
        Assert.HasCount(1, Directory.GetFileSystemEntries(root.Resolve("destination")));
    }

    private static async Task<FileOperationOutcome> CopyIntoConflictAsync(TestOwnedTemporaryRoot root, string name)
    {
        _ = root.CreateDirectory("source");
        FileSystemPath source = WindowsLocalOperationTestContext.ParsePath(root.WriteFile("source\\" + name, "incoming"));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("destination"));
        _ = root.WriteFile("destination\\" + name, "existing");
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter(new DeterministicIdentifierSource()));
        CopyRequest request = (CopyRequest)((FileOperationRequestAccepted)CopyRequest.Create([source], destination)).Request;
        return await gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), CancellationToken.None);
    }
}
