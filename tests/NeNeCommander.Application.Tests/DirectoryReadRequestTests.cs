using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves directory read requests carry a validated location and entry boundary.</summary>
[TestClass]
public sealed class DirectoryReadRequestTests
{
    /// <summary>Proves the inclusive boundary range is accepted verbatim.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(DirectoryListing.EntryBoundaryLimit)]
    public void CreateWhenEntryBoundaryIsWithinRangeAccepted(int entryBoundary)
    {
        FileSystemPath location = ParsePath("C:\\projects");

        DirectoryReadRequestCreation outcome = DirectoryReadRequest.Create(location, entryBoundary);

        DirectoryReadRequest request = Assert.IsInstanceOfType<DirectoryReadRequestAccepted>(outcome).Request;
        Assert.AreSame(location, request.Location);
        Assert.AreEqual(entryBoundary, request.EntryBoundary);
    }

    /// <summary>Proves boundaries outside the fixed range cannot reach an adapter.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(DirectoryListing.EntryBoundaryLimit + 1)]
    public void CreateWhenEntryBoundaryIsOutOfRangeRejected(int entryBoundary)
    {
        DirectoryReadRequestCreation outcome = DirectoryReadRequest.Create(ParsePath("C:\\projects"), entryBoundary);

        _ = Assert.IsInstanceOfType<DirectoryReadRequestRejected>(outcome);
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
