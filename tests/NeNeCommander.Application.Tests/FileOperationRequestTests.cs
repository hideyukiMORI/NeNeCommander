using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves request construction and boundary validation.</summary>
[TestClass]
public sealed class FileOperationRequestTests
{
    /// <summary>Proves duplicate sources cannot enter the gateway.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public void CreateWhenSourcesAreDuplicateDuplicateSourceRejection()
    {
        FileSystemPath source = ParsePath("C:\\Source");
        FileSystemPath caseVariant = ParsePath("c:\\source");

        FileOperationRequestCreation outcome = MoveRequest.Create(
            [source, caseVariant],
            ParsePath("D:\\destination"));

        FileOperationRequestRejected rejected = Assert.IsInstanceOfType<FileOperationRequestRejected>(outcome);
        Assert.AreSame(FileOperationRequestFailureKind.DuplicateSource, rejected.Kind);
    }

    /// <summary>Proves operation batches have a fixed upper boundary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public void CreateWhenSourceCountExceedsBoundaryTooManySourcesRejection()
    {
        FileSystemPath source = ParsePath("C:\\source");
        FileSystemPath[] sources = [.. Enumerable.Repeat(source, 10001)];

        FileOperationRequestCreation outcome = DeleteRequest.Create(sources, null);

        FileOperationRequestRejected rejected = Assert.IsInstanceOfType<FileOperationRequestRejected>(outcome);
        Assert.AreSame(FileOperationRequestFailureKind.TooManySources, rejected.Kind);
    }

    /// <summary>Proves the exact operation batch limit remains admitted to normal validation.</summary>
    [TestMethod]
    public void CreateWhenSourceCountEqualsBoundaryContinuesToDuplicateValidation()
    {
        FileSystemPath source = ParsePath("C:\\source");
        FileSystemPath[] sources = [.. Enumerable.Repeat(source, 10000)];

        FileOperationRequestCreation outcome = DeleteRequest.Create(sources, null);

        FileOperationRequestRejected rejected = Assert.IsInstanceOfType<FileOperationRequestRejected>(outcome);
        Assert.AreSame(FileOperationRequestFailureKind.DuplicateSource, rejected.Kind);
    }

    /// <summary>Proves requests own the accepted source snapshot.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public void CreateWhenCallerChangesSourceArrayRequestRetainsFrozenSources()
    {
        FileSystemPath original = ParsePath("C:\\original");
        FileSystemPath[] sources = [original];
        MoveRequest request = RequireMove(MoveRequest.Create(sources, ParsePath("D:\\destination")));

        sources[0] = ParsePath("C:\\replacement");

        Assert.AreSame(original, request.Sources[0]);
    }

    /// <summary>Proves empty source sets are rejected uniformly.</summary>
    [TestMethod]
    public void CreateWhenSourcesAreEmptyEmptySourcesRejection()
    {
        FileOperationRequestCreation move = MoveRequest.Create([], ParsePath("D:\\destination"));
        FileOperationRequestCreation delete = DeleteRequest.Create([], null);

        Assert.AreSame(
            FileOperationRequestFailureKind.EmptySources,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(move).Kind);
        Assert.AreSame(
            FileOperationRequestFailureKind.EmptySources,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(delete).Kind);
    }

    /// <summary>Proves null source entries are rejected.</summary>
    [TestMethod]
    public void CreateWhenSourceContainsNullNullSourceRejection()
    {
        FileSystemPath[] sources = new FileSystemPath[1];

        FileOperationRequestCreation outcome = DeleteRequest.Create(sources, null);

        Assert.AreSame(
            FileOperationRequestFailureKind.NullSource,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(outcome).Kind);
    }

    /// <summary>Proves a source cannot also be its destination.</summary>
    [TestMethod]
    public void CreateWhenDestinationMatchesSourceDestinationIsSourceRejection()
    {
        FileSystemPath source = ParsePath("C:\\Source");

        FileOperationRequestCreation outcome = MoveRequest.Create([source], ParsePath("c:\\source"));

        Assert.AreSame(
            FileOperationRequestFailureKind.DestinationIsSource,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(outcome).Kind);
    }

    /// <summary>Proves a copy shares the transfer validation and freezes its destination.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public void CreateWhenCopyIsRequestedValidatesLikeMoveAndFreezesDestination()
    {
        FileSystemPath source = ParsePath("C:\\Source");
        FileSystemPath destination = ParsePath("D:\\destination");

        FileOperationRequestCreation sameAsSource = CopyRequest.Create([source], ParsePath("c:\\source"));
        FileOperationRequestCreation duplicated = CopyRequest.Create([source, ParsePath("c:\\source")], destination);
        FileOperationRequestCreation empty = CopyRequest.Create([], destination);
        CopyRequest accepted = RequireCopy(CopyRequest.Create([source], destination));

        Assert.AreSame(
            FileOperationRequestFailureKind.DestinationIsSource,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(sameAsSource).Kind);
        Assert.AreSame(
            FileOperationRequestFailureKind.DuplicateSource,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(duplicated).Kind);
        Assert.AreSame(
            FileOperationRequestFailureKind.EmptySources,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(empty).Kind);
        Assert.AreSame(destination, accepted.Destination);
        Assert.AreSame(source, accepted.Sources[0]);
    }

    /// <summary>Proves missing provider identities are rejected.</summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ParseWhenFileIdentityIsMissingRejected(string? input)
    {
        _ = Assert.IsInstanceOfType<FileIdentityRejected>(FileIdentity.Parse(input));
    }

    /// <summary>Proves valid provider identities remain opaque and verbatim.</summary>
    [TestMethod]
    public void ParseWhenFileIdentityIsValidAcceptedVerbatim()
    {
        FileIdentityAccepted accepted = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("provider:42"));

        Assert.AreEqual("provider:42", accepted.Identity.Value);
    }

    /// <summary>Proves provider identities have a fixed size boundary.</summary>
    [TestMethod]
    public void ParseWhenFileIdentityExceedsBoundaryRejected()
    {
        _ = Assert.IsInstanceOfType<FileIdentityRejected>(FileIdentity.Parse(new string('x', 513)));
    }

    /// <summary>Proves the exact provider-identity size boundary remains accepted.</summary>
    [TestMethod]
    public void ParseWhenFileIdentityEqualsBoundaryAccepted()
    {
        _ = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse(new string('x', 512)));
    }

    private static CopyRequest RequireCopy(FileOperationRequestCreation outcome)
    {
        FileOperationRequest request = Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request;
        return Assert.IsInstanceOfType<CopyRequest>(request);
    }

    private static MoveRequest RequireMove(FileOperationRequestCreation outcome)
    {
        FileOperationRequest request = Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request;
        return Assert.IsInstanceOfType<MoveRequest>(request);
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
