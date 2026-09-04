using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves rename requests freeze the source and derive the target inside the source's own parent.</summary>
[TestClass]
public sealed class RenameRequestTests
{
    /// <summary>Proves a valid name yields a request whose sole source is the entry and whose target is its sibling.</summary>
    [TestMethod]
    public void CreateWhenNameIsValidFreezesSourceAndSiblingTarget()
    {
        FileSystemPath source = ParsePath("C:\\Users\\xi\\notes.txt");

        FileOperationRequestCreation outcome = RenameRequest.Create(source, "plans.txt");

        RenameRequest request = Assert.IsInstanceOfType<RenameRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
        Assert.AreSame(source, request.Source);
        Assert.AreEqual("C:\\Users\\xi\\plans.txt", request.Target.CanonicalText);
        Assert.HasCount(1, request.Sources);
        Assert.AreSame(source, request.Sources[0]);
    }

    /// <summary>Proves a change of letter case alone is a real rename, so filesystem identity never rejects it.</summary>
    [TestMethod]
    public void CreateWhenOnlyCaseChangesAcceptsTheRequest()
    {
        FileSystemPath source = ParsePath("C:\\Users\\xi\\notes.txt");

        FileOperationRequestCreation outcome = RenameRequest.Create(source, "Notes.TXT");

        RenameRequest request = Assert.IsInstanceOfType<RenameRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
        Assert.AreEqual("C:\\Users\\xi\\Notes.TXT", request.Target.CanonicalText);
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(request.Source, request.Target));
    }

    /// <summary>Proves a name the domain rejects never becomes a request.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    [DataRow("")]
    [DataRow(".")]
    [DataRow("..")]
    [DataRow("a\\b")]
    [DataRow("a/b")]
    [DataRow("CON")]
    public void CreateWhenNameIsRejectedByDomainInvalidNameRejection(string name)
    {
        FileOperationRequestCreation outcome = RenameRequest.Create(ParsePath("C:\\Users\\xi\\notes.txt"), name);

        Assert.AreSame(
            FileOperationRequestFailureKind.InvalidName,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(outcome).Kind);
    }

    /// <summary>Proves a provider root has no parent to be renamed in and is refused before any name check.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    [DataRow("C:\\")]
    [DataRow("\\\\server\\share")]
    [DataRow("\\\\wsl.localhost\\Ubuntu")]
    public void CreateWhenSourceIsProviderRootSourceIsRootRejection(string root)
    {
        FileOperationRequestCreation outcome = RenameRequest.Create(ParsePath(root), "renamed");

        Assert.AreSame(
            FileOperationRequestFailureKind.SourceIsRoot,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(outcome).Kind);
    }

    /// <summary>Proves an unchanged name is refused so the gateway never starts an operation with nothing to do.</summary>
    [TestMethod]
    public void CreateWhenNameIsUnchangedDestinationIsSourceRejection()
    {
        FileOperationRequestCreation outcome = RenameRequest.Create(
            ParsePath("C:\\Users\\xi\\notes.txt"),
            "notes.txt");

        Assert.AreSame(
            FileOperationRequestFailureKind.DestinationIsSource,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(outcome).Kind);
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
