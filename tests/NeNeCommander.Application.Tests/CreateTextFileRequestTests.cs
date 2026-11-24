using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves empty-file requests freeze the location and derive the target through domain rules.</summary>
[TestClass]
public sealed class CreateTextFileRequestTests
{
    /// <summary>Proves a valid name yields a request whose sole source is the location and whose target is its direct child.</summary>
    [TestMethod]
    public void CreateWhenNameIsValidFreezesLocationAndTarget()
    {
        FileSystemPath location = ParsePath("C:\\Users\\xi");

        FileOperationRequestCreation outcome = CreateTextFileRequest.Create(location, "notes.txt");

        CreateTextFileRequest request = Assert.IsInstanceOfType<CreateTextFileRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
        Assert.AreSame(location, request.Location);
        Assert.AreEqual("C:\\Users\\xi\\notes.txt", request.Target.CanonicalText);
        Assert.HasCount(1, request.Sources);
        Assert.AreSame(location, request.Sources[0]);
    }

    /// <summary>Proves a name the domain rejects never becomes a request.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    [DataRow("")]
    [DataRow("..")]
    [DataRow(".")]
    [DataRow("a/b")]
    [DataRow("a:b")]
    [DataRow("a\\b")]
    [DataRow("CON")]
    [DataRow("COM¹")]
    [DataRow("lpt².txt")]
    public void CreateWhenNameIsRejectedByDomainInvalidNameRejection(string name)
    {
        FileOperationRequestCreation outcome = CreateTextFileRequest.Create(ParsePath("C:\\Users"), name);

        Assert.AreSame(
            FileOperationRequestFailureKind.InvalidName,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(outcome).Kind);
    }

    /// <summary>Proves the submitted extension and Unicode are preserved without suffix or text normalization.</summary>
    [TestMethod]
    [DataRow("notes")]
    [DataRow("記録.txt")]
    [DataRow("notes.log")]
    public void CreateWhenNameHasChosenExtensionKeepsExactName(string name)
    {
        FileSystemPath location = ParsePath("C:\\work");

        CreateTextFileRequest request = Assert.IsInstanceOfType<CreateTextFileRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(CreateTextFileRequest.Create(location, name)).Request);

        Assert.AreEqual(location.CanonicalText + "\\" + name, request.Target.CanonicalText);
    }

    /// <summary>Proves Linux names retain exact spelling and are not reinterpreted as Windows devices.</summary>
    [TestMethod]
    [DataRow("COM1")]
    [DataRow("foo.")]
    [DataRow("notes.txt")]
    [DataRow("NOTES.txt")]
    public void CreateWhenLocationIsWslPreservesProviderSpecificName(string name)
    {
        WslPath location = Assert.IsInstanceOfType<WslPath>(ParsePath(@"\\wsl.localhost\Ubuntu\tmp"));

        CreateTextFileRequest request = Assert.IsInstanceOfType<CreateTextFileRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(CreateTextFileRequest.Create(location, name)).Request);

        Assert.AreSame(location, request.Location);
        Assert.AreEqual("/tmp/" + name, Assert.IsInstanceOfType<WslPath>(request.Target).LinuxPath);
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(location, request.Target.Parent));
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
