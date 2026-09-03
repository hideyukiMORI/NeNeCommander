using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Domain.Tests;

/// <summary>Proves child derivation applies the same segment rules as parsing and never leaves the location.</summary>
[TestClass]
public sealed class FileSystemPathChildTests
{
    /// <summary>Proves a valid name becomes the direct child of a root and of a nested location.</summary>
    [TestMethod]
    public void ChildWhenNameIsValidReturnsDirectChild()
    {
        FileSystemPath root = ParsePath("C:\\");
        FileSystemPath nested = ParsePath("C:\\Users\\xi");

        FileSystemPath rootChild = RequireChild(root, "New Folder");
        FileSystemPath nestedChild = RequireChild(nested, "docs.v2");

        Assert.AreEqual("C:\\New Folder", rootChild.CanonicalText);
        Assert.AreEqual("C:\\Users\\xi\\docs.v2", nestedChild.CanonicalText);
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(nested, RequireParent(nestedChild)));
    }

    /// <summary>Proves UNC and WSL locations derive children with their own provider identity.</summary>
    [TestMethod]
    public void ChildWhenLocationIsUncOrWslKeepsProvider()
    {
        FileSystemPath unc = ParsePath("\\\\server\\share\\dir");
        FileSystemPath wsl = ParsePath("\\\\wsl$\\Ubuntu\\home");

        FileSystemPath uncChild = RequireChild(unc, "child");
        FileSystemPath wslChild = RequireChild(wsl, "CON");

        _ = Assert.IsInstanceOfType<WindowsUncPath>(uncChild);
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(unc, RequireParent(uncChild)));
        _ = Assert.IsInstanceOfType<WslPath>(wslChild);
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(wsl, RequireParent(wslChild)));
    }

    /// <summary>Proves an absent or empty name is rejected as empty.</summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void ChildWhenNameIsMissingRejectedAsEmpty(string? name)
    {
        PathParseOutcome outcome = ParsePath("C:\\Users").Child(name);

        Assert.AreSame(PathParseFailureKind.Empty, Assert.IsInstanceOfType<PathParseFailure>(outcome).Kind);
    }

    /// <summary>Proves separators, dot segments, reserved names, illegal characters, and trailing dots or spaces never escape or corrupt the location.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-001")]
    [TestProperty("ThreatId", "ADV-015")]
    [DataRow("a\\b")]
    [DataRow("a/b")]
    [DataRow(".")]
    [DataRow("..")]
    [DataRow("CON")]
    [DataRow("nul.txt")]
    [DataRow("a:b")]
    [DataRow("trailing.")]
    [DataRow("trailing ")]
    [DataRow(" ")]
    [DataRow("tab\tname")]
    public void ChildWhenNameIsHostileRejectedAsInvalidSegment(string name)
    {
        PathParseOutcome outcome = ParsePath("C:\\Users").Child(name);

        Assert.AreSame(PathParseFailureKind.InvalidSegment, Assert.IsInstanceOfType<PathParseFailure>(outcome).Kind);
    }

    /// <summary>Proves a name that pushes the path past the length boundary is rejected as too long.</summary>
    [TestMethod]
    public void ChildWhenNameExceedsPathBoundaryRejectedAsTooLong()
    {
        PathParseOutcome outcome = ParsePath("C:\\Users").Child(new string('a', 40000));

        Assert.AreSame(PathParseFailureKind.TooLong, Assert.IsInstanceOfType<PathParseFailure>(outcome).Kind);
    }

    private static FileSystemPath RequireChild(FileSystemPath location, string name)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(location.Child(name)).Path;
    }

    private static FileSystemPath RequireParent(FileSystemPath path)
    {
        FileSystemPath? parent = path.Parent;
        Assert.IsNotNull(parent);
        return parent;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
