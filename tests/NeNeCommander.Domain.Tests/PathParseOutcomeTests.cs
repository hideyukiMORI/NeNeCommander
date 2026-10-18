using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Domain.Tests;

/// <summary>Proves known parse records retain copy, equality and payload after external case closure.</summary>
[TestClass]
public sealed class PathParseOutcomeTests
{
    /// <summary>Proves successful outcomes copy through root and concrete views without changing the path.</summary>
    [TestMethod]
    public void WithWhenParseSucceedsPreservesRootAndConcretePayload()
    {
        PathParseOutcome original = FileSystemPath.Parse("C:/root/file2");
        PathParseSuccess concrete = Assert.IsInstanceOfType<PathParseSuccess>(original);

        PathParseOutcome copied = original with { };
        PathParseSuccess concreteCopy = concrete with { };
        FileSystemPath pathCopy = concrete.Path with { };

        Assert.AreNotSame(original, copied);
        Assert.AreEqual(original, copied);
        Assert.AreEqual(original.GetHashCode(), copied.GetHashCode());
        Assert.AreSame(concrete.Path, Assert.IsInstanceOfType<PathParseSuccess>(copied).Path);
        Assert.AreNotSame(concrete, concreteCopy);
        Assert.AreEqual(concrete, concreteCopy);
        Assert.AreSame(concrete.Path, concreteCopy.Path);
        Assert.AreEqual(concrete.Path, pathCopy);
        Assert.AreNotEqual(original, FileSystemPath.Parse("C:/root/file3"));
    }

    /// <summary>Proves failure payloads and private nested reason cases retain their value distinction.</summary>
    [TestMethod]
    public void WithWhenParseFailsPreservesReasonAndCaseDistinction()
    {
        PathParseOutcome original = FileSystemPath.Parse(string.Empty);
        PathParseFailure concrete = Assert.IsInstanceOfType<PathParseFailure>(original);

        PathParseOutcome copied = original with { };
        PathParseFailure concreteCopy = concrete with { };
        PathParseFailureKind reasonCopy = concrete.Kind with { };

        Assert.AreEqual(original, copied);
        Assert.AreEqual(concrete, concreteCopy);
        Assert.AreSame(PathParseFailureKind.Empty, concreteCopy.Kind);
        Assert.AreEqual(concrete.Kind, reasonCopy);
        Assert.AreEqual(concrete.Kind.GetHashCode(), reasonCopy.GetHashCode());
        Assert.AreNotEqual(PathParseFailureKind.Relative, reasonCopy);
        Assert.AreNotEqual(original, FileSystemPath.Parse("C:/root"));
    }
}
