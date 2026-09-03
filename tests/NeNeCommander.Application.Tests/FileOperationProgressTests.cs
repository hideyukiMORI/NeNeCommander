using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the progress value keeps its counting invariant.</summary>
[TestClass]
public sealed class FileOperationProgressTests
{
    /// <summary>Proves every pair inside the invariant is accepted verbatim.</summary>
    [TestMethod]
    public void CreateWhenPairIsInsideInvariantKeepsBothCounts()
    {
        FileOperationProgress start = FileOperationProgress.Create(0, 1);
        FileOperationProgress end = FileOperationProgress.Create(3, 3);

        Assert.AreEqual(0, start.Completed);
        Assert.AreEqual(1, start.Total);
        Assert.AreEqual(3, end.Completed);
        Assert.AreEqual(3, end.Total);
    }

    /// <summary>Proves a request without sources, a negative count, and an overshoot are gateway defects.</summary>
    [TestMethod]
    [DataRow(0, 0, "total")]
    [DataRow(-1, 1, "completed")]
    [DataRow(2, 1, "completed")]
    public void CreateWhenPairViolatesInvariantThrows(int completed, int total, string parameterName)
    {
        ArgumentOutOfRangeException failure = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => FileOperationProgress.Create(completed, total));

        Assert.AreEqual(parameterName, failure.ParamName);
    }
}
