using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves initial overwrite admission and batch consent through the real gateway and native adapter.</summary>
[TestClass]
public sealed class WindowsOverwriteInitialPreflightTests
{
    /// <summary>Native qualification carries the original object through copy, verification and owned cleanup.</summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("desired")]
    public async Task ResumeAsyncWhenNativeConflictIsConfirmedCopiesIntoExactObject(string content)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath source = PathOf(root.WriteFile("source", content));
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        FileEntrySnapshot original = WindowsLocalOperationTestContext.LocalSnapshot(root.WriteFile("destination/source", "original"));
        DeterministicIdentifierSource identifiers = new();
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter(identifiers));
        FileOperationOutcome awaiting = await ExecuteAsync(gateway, [source], destination);
        TransferConflict conflict = awaiting.Conflicts!.Conflicts[0];
        Assert.AreEqual(original.Identity, conflict.Target.OverwriteEvidence!.Target.Observation.Entry.Identity);
        Assert.IsEmpty(awaiting.ProviderSteps);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.AreEqual("original", File.ReadAllText(original.Path.CanonicalText));

        FileOperationOutcome result = await ResumeAsync(gateway, awaiting, TransferConflictDecision.OverwriteContents, TransferConflictScope.Current);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, result.Completion);
        Assert.HasCount(2, result.Effects);
        Assert.AreEqual(conflict.Target.OverwriteEvidence.Target.Observation.Entry.ObjectIdentity,
            WindowsLocalEntryIdentity.Capture((WindowsLocalPath)original.Path, new FileInfo(original.Path.CanonicalText)).ObjectIdentity);
        Assert.AreEqual(content, File.ReadAllText(original.Path.CanonicalText));
        Assert.AreEqual(content, File.ReadAllText(source.CanonicalText));
        Assert.HasCount(1, Directory.GetFiles(destination.CanonicalText));
        Assert.AreEqual(1, identifiers.IssuedCount);
    }

    /// <summary>A target reserved by an ordinary batch copy is not an existing file that can be overwritten.</summary>
    [TestMethod]
    [DataRow("current")]
    [DataRow("all")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task ResumeAsyncWhenOnlyBatchReservationExistsRefusesOverwriteWithoutEffects(string scope)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath[] sources = DuplicateSources(root);
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        DeterministicIdentifierSource identifiers = new();
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter(identifiers));
        FileOperationOutcome awaiting = await ExecuteAsync(gateway, sources, destination);
        Assert.HasCount(1, awaiting.Conflicts!.Conflicts);
        AssertReserved(awaiting.Conflicts.Conflicts[0]);

        FileOperationOutcome result = await ResumeAsync(gateway, awaiting, TransferConflictDecision.OverwriteContents,
            scope == "all" ? TransferConflictScope.All : TransferConflictScope.Current);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, result.Failure);
        Assert.IsEmpty(result.Effects);
        Assert.IsEmpty(result.ProviderSteps);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.IsEmpty(Directory.GetFileSystemEntries(destination.CanonicalText));
        Assert.AreEqual("first", File.ReadAllText(sources[0].CanonicalText));
        Assert.AreEqual("second", File.ReadAllText(sources[1].CanonicalText));
    }

    /// <summary>Duplicate existing targets cannot both acquire permission from a single All decision.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenDuplicateTargetsExistLeavesSecondUnresolvedBeforeAnyCopy()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath[] sources = DuplicateSources(root);
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        string target = root.WriteFile("destination/source", "original");
        DeterministicIdentifierSource identifiers = new();
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter(identifiers));
        FileOperationOutcome awaiting = await ExecuteAsync(gateway, sources, destination);
        Assert.HasCount(2, awaiting.Conflicts!.Conflicts);
        Assert.IsNotNull(awaiting.Conflicts.Conflicts[0].Target.OverwriteEvidence);
        AssertReserved(awaiting.Conflicts.Conflicts[1]);

        FileOperationOutcome unresolved = await ResumeAsync(gateway, awaiting, TransferConflictDecision.OverwriteContents, TransferConflictScope.All);

        Assert.HasCount(1, unresolved.Conflicts!.Conflicts);
        AssertReserved(unresolved.Conflicts.Conflicts[0]);
        Assert.AreEqual(sources[1], unresolved.Conflicts.Conflicts[0].Source.Path);
        Assert.IsEmpty(unresolved.Effects);
        Assert.IsEmpty(unresolved.ProviderSteps);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.AreEqual("original", File.ReadAllText(target));
        FileOperationOutcome result = await ResumeAsync(gateway, unresolved, TransferConflictDecision.Skip, TransferConflictScope.Current);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, result.Completion);
        Assert.HasCount(1, result.NotTransferred);
        Assert.AreEqual(sources[1], result.NotTransferred[0]);
        Assert.AreEqual("first", File.ReadAllText(target));
        Assert.AreEqual("second", File.ReadAllText(sources[1].CanonicalText));
        Assert.HasCount(1, Directory.GetFiles(destination.CanonicalText));
    }

    /// <summary>All applies to each distinct disclosed eligible target and preserves both sources for Copy.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenDistinctEligibleTargetsAreDisclosedOverwritesBoth()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath first = PathOf(root.WriteFile("first", "one"));
        FileSystemPath second = PathOf(root.WriteFile("second", "two"));
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        _ = root.WriteFile("destination/first", "old one");
        _ = root.WriteFile("destination/second", "old two");
        DeterministicIdentifierSource identifiers = new();
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter(identifiers));
        FileOperationOutcome awaiting = await ExecuteAsync(gateway, [first, second], destination);
        Assert.HasCount(2, awaiting.Conflicts!.Conflicts);

        FileOperationOutcome result = await ResumeAsync(gateway, awaiting, TransferConflictDecision.OverwriteContents, TransferConflictScope.All);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, result.Completion);
        Assert.HasCount(4, result.Effects);
        Assert.AreEqual(2, identifiers.IssuedCount);
        Assert.AreEqual("one", File.ReadAllText(root.Resolve("destination/first")));
        Assert.AreEqual("two", File.ReadAllText(root.Resolve("destination/second")));
        Assert.AreEqual("one", File.ReadAllText(first.CanonicalText));
        Assert.AreEqual("two", File.ReadAllText(second.CanonicalText));
        Assert.HasCount(2, Directory.GetFiles(destination.CanonicalText));
    }

    /// <summary>A later conflict requires a new disclosure even after the first response selected All.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task ResumeAsyncWhenLaterConflictAppearsDoesNotInheritEarlierAllPermission()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath first = PathOf(root.WriteFile("first", "one"));
        FileSystemPath second = PathOf(root.WriteFile("second", "two"));
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        _ = root.WriteFile("destination/first", "old one");
        DeterministicIdentifierSource identifiers = new();
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter(identifiers));
        FileOperationOutcome awaiting = await ExecuteAsync(gateway, [first, second], destination);
        Assert.HasCount(1, awaiting.Conflicts!.Conflicts);
        _ = root.WriteFile("destination/second", "later");

        FileOperationOutcome unresolved = await ResumeAsync(gateway, awaiting, TransferConflictDecision.OverwriteContents, TransferConflictScope.All);

        Assert.HasCount(1, unresolved.Conflicts!.Conflicts);
        Assert.AreEqual(second, unresolved.Conflicts.Conflicts[0].Source.Path);
        Assert.IsNotNull(unresolved.Conflicts.Conflicts[0].Target.OverwriteEvidence);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.IsEmpty(unresolved.ProviderSteps);
        Assert.AreEqual("old one", File.ReadAllText(root.Resolve("destination/first")));
        Assert.AreEqual("later", File.ReadAllText(root.Resolve("destination/second")));
        FileOperationOutcome result = await ResumeAsync(gateway, unresolved, TransferConflictDecision.Skip, TransferConflictScope.Current);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, result.Completion);
        Assert.AreEqual("one", File.ReadAllText(root.Resolve("destination/first")));
        Assert.AreEqual("later", File.ReadAllText(root.Resolve("destination/second")));
        Assert.HasCount(1, result.NotTransferred);
    }

    /// <summary>KeepBoth candidate replacement still qualifies only the original existing file.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenKeepBothCandidateWasTakenNewConflictRequiresFreshOverwriteConsent()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath source = PathOf(root.WriteFile("source", "desired"));
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        _ = root.WriteFile("destination/source", "original");
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter(new DeterministicIdentifierSource()));
        FileOperationOutcome awaiting = await ExecuteAsync(gateway, [source], destination);
        _ = root.WriteFile("destination/source (2)", "foreign");

        FileOperationOutcome unresolved = await ResumeAsync(gateway, awaiting, TransferConflictDecision.KeepBoth, TransferConflictScope.All);

        TransferConflict conflict = unresolved.Conflicts!.Conflicts[0];
        Assert.IsNotNull(conflict.Target.OverwriteEvidence);
        Assert.AreEqual(root.Resolve("destination/source (3)"), conflict.KeepBothCandidate.CanonicalText);
        Assert.IsEmpty(unresolved.ProviderSteps);
        FileOperationOutcome result = await ResumeAsync(gateway, unresolved, TransferConflictDecision.OverwriteContents, TransferConflictScope.Current);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, result.Completion);
        Assert.AreEqual("desired", File.ReadAllText(root.Resolve("destination/source")));
        Assert.AreEqual("foreign", File.ReadAllText(root.Resolve("destination/source (2)")));
        Assert.IsFalse(File.Exists(root.Resolve("destination/source (3)")));
    }

    private static FileSystemPath[] DuplicateSources(TestOwnedTemporaryRoot root)
    {
        _ = root.CreateDirectory("one");
        _ = root.CreateDirectory("two");
        return [PathOf(root.WriteFile("one/source", "first")), PathOf(root.WriteFile("two/source", "second"))];
    }

    private static void AssertReserved(TransferConflict conflict)
    {
        Assert.AreSame(TransferOverwriteUnavailableReason.ReservedTarget,
            Assert.IsInstanceOfType<TransferOverwriteCapability.UnavailableOverwrite>(conflict.Target.Overwrite).Reason);
        Assert.IsNull(conflict.Target.OverwriteEvidence);
        CollectionAssert.DoesNotContain(System.Linq.Enumerable.ToArray(conflict.AllowedDecisions), TransferConflictDecision.OverwriteContents);
    }

    private static Task<FileOperationOutcome> ExecuteAsync(FileOperationGateway gateway, FileSystemPath[] sources, FileSystemPath destination)
    {
        CopyRequest request = Assert.IsInstanceOfType<CopyRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(CopyRequest.Create(sources, destination)).Request);
        return gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), CancellationToken.None);
    }

    private static Task<FileOperationOutcome> ResumeAsync(FileOperationGateway gateway, FileOperationOutcome outcome,
        TransferConflictDecision decision, TransferConflictScope scope)
    {
        return gateway.ResumeAsync(outcome.Continuation!, TransferConflictResponse.Create(decision, scope),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);
    }

    private static FileSystemPath PathOf(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
