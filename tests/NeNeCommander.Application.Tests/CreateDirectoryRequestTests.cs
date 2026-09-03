using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves directory-creation requests freeze the location and derive the target through domain rules.</summary>
[TestClass]
public sealed class CreateDirectoryRequestTests
{
    /// <summary>Proves a valid name yields a request whose sole source is the location and whose target is its direct child.</summary>
    [TestMethod]
    public void CreateWhenNameIsValidFreezesLocationAndTarget()
    {
        FileSystemPath location = ParsePath("C:\\Users\\xi");

        FileOperationRequestCreation outcome = CreateDirectoryRequest.Create(location, "New Folder");

        CreateDirectoryRequest request = Assert.IsInstanceOfType<CreateDirectoryRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
        Assert.AreSame(location, request.Location);
        Assert.AreEqual("C:\\Users\\xi\\New Folder", request.Target.CanonicalText);
        Assert.HasCount(1, request.Sources);
        Assert.AreSame(location, request.Sources[0]);
    }

    /// <summary>Proves a name the domain rejects never becomes a request.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    [DataRow("")]
    [DataRow("..")]
    [DataRow("a\\b")]
    [DataRow("CON")]
    public void CreateWhenNameIsRejectedByDomainInvalidNameRejection(string name)
    {
        FileOperationRequestCreation outcome = CreateDirectoryRequest.Create(ParsePath("C:\\Users"), name);

        Assert.AreSame(
            FileOperationRequestFailureKind.InvalidName,
            Assert.IsInstanceOfType<FileOperationRequestRejected>(outcome).Kind);
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
