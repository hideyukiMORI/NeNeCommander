using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves containment records keep their existing copy and value contracts without I/O.</summary>
[TestClass]
public sealed class PathContainmentOutcomeTests
{
    /// <summary>Proves accepted path payload and equality survive root and concrete copies.</summary>
    [TestMethod]
    public void WithWhenPathIsContainedPreservesExactPath()
    {
        FileSystemPath root = Parse("C:/root");
        FileSystemPath path = Parse("C:/root/file");
        PathContainmentOutcome original = ProviderPathContainment.Evaluate(root, path);
        ContainedPath concrete = Assert.IsInstanceOfType<ContainedPath>(original);

        PathContainmentOutcome copied = original with { };
        ContainedPath concreteCopy = concrete with { };

        Assert.AreNotSame(original, copied);
        Assert.AreEqual(original, copied);
        Assert.AreEqual(original.GetHashCode(), copied.GetHashCode());
        Assert.AreSame(path, Assert.IsInstanceOfType<ContainedPath>(copied).Path);
        Assert.AreNotSame(concrete, concreteCopy);
        Assert.AreEqual(concrete, concreteCopy);
        Assert.AreSame(path, concreteCopy.Path);
        Assert.AreNotEqual(original, ProviderPathContainment.Evaluate(root, Parse("C:/root/other")));
    }

    /// <summary>Proves a rejected case and its nested reason copy without becoming another rejection.</summary>
    [TestMethod]
    public void WithWhenPathIsRejectedPreservesReason()
    {
        PathContainmentOutcome original = ProviderPathContainment.Evaluate(Parse("C:/root"), Parse("C:/outside"));
        RejectedPathContainment concrete = Assert.IsInstanceOfType<RejectedPathContainment>(original);

        PathContainmentOutcome copied = original with { };
        RejectedPathContainment concreteCopy = concrete with { };
        PathContainmentFailureKind reasonCopy = concrete.Kind with { };

        Assert.AreEqual(original, copied);
        Assert.AreEqual(concrete, concreteCopy);
        Assert.AreSame(PathContainmentFailureKind.OutsideRoot, concreteCopy.Kind);
        Assert.AreEqual(concrete.Kind, reasonCopy);
        Assert.AreEqual(concrete.Kind.GetHashCode(), reasonCopy.GetHashCode());
        Assert.AreNotEqual(PathContainmentFailureKind.ProviderMismatch, reasonCopy);
    }

    private static FileSystemPath Parse(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
