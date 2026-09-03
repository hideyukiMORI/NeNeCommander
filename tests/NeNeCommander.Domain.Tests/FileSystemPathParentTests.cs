using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Domain.Tests;

/// <summary>Proves parent derivation stops at each provider root and preserves identity rules.</summary>
[TestClass]
public sealed class FileSystemPathParentTests
{
    /// <summary>Proves Windows local parents climb to the drive root and then stop.</summary>
    [TestMethod]
    public void ParentWhenPathIsWindowsLocalClimbsToDriveRootThenAbsent()
    {
        FileSystemPath nested = ParsePath("C:\\Users\\xi\\Docs");

        FileSystemPath level1 = RequireParent(nested);
        FileSystemPath level2 = RequireParent(level1);
        FileSystemPath root = RequireParent(level2);

        Assert.AreEqual("C:\\Users\\xi", level1.CanonicalText);
        Assert.AreEqual("C:\\Users", level2.CanonicalText);
        Assert.AreEqual("C:\\", root.CanonicalText);
        Assert.AreEqual("C", Assert.IsInstanceOfType<WindowsLocalPath>(root).Drive[..1]);
        Assert.IsNull(root.Parent);
    }

    /// <summary>Proves UNC parents stop at the share root and keep server and share.</summary>
    [TestMethod]
    public void ParentWhenPathIsUncStopsAtShareRoot()
    {
        FileSystemPath nested = ParsePath("\\\\server\\share\\a\\b");

        FileSystemPath level1 = RequireParent(nested);
        FileSystemPath root = RequireParent(level1);

        Assert.AreEqual("\\\\server\\share\\a", level1.CanonicalText);
        Assert.AreEqual("\\\\server\\share\\", root.CanonicalText);
        WindowsUncPath unc = Assert.IsInstanceOfType<WindowsUncPath>(root);
        Assert.AreEqual("server", unc.Server);
        Assert.AreEqual("share", unc.Share);
        Assert.IsNull(root.Parent);
    }

    /// <summary>Proves WSL parents keep the distribution and derive the Linux path in step.</summary>
    [TestMethod]
    public void ParentWhenPathIsWslDerivesLinuxPathAndStopsAtDistributionRoot()
    {
        FileSystemPath nested = ParsePath("\\\\wsl$\\Ubuntu\\home\\xi");

        FileSystemPath level1 = RequireParent(nested);
        FileSystemPath root = RequireParent(level1);

        WslPath home = Assert.IsInstanceOfType<WslPath>(level1);
        Assert.AreEqual("\\\\wsl.localhost\\Ubuntu\\home", home.CanonicalText);
        Assert.AreEqual("/home", home.LinuxPath);
        WslPath distributionRoot = Assert.IsInstanceOfType<WslPath>(root);
        Assert.AreEqual("\\\\wsl.localhost\\Ubuntu\\", distributionRoot.CanonicalText);
        Assert.AreEqual("/", distributionRoot.LinuxPath);
        Assert.AreEqual("Ubuntu", distributionRoot.DistributionName);
        Assert.IsNull(root.Parent);
    }

    /// <summary>Proves derived parents share identity with parsed equivalents.</summary>
    [TestMethod]
    public void ParentWhenComparedWithParsedParentSharesProviderIdentity()
    {
        FileSystemPath local = RequireParent(ParsePath("c:\\Projects\\nene"));
        FileSystemPath wsl = RequireParent(ParsePath("\\\\wsl.localhost\\ubuntu\\home\\Xi"));

        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(local, ParsePath("C:\\PROJECTS")));
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(wsl, ParsePath("\\\\wsl.localhost\\Ubuntu\\home")));
        Assert.IsFalse(FileSystemPathIdentityComparer.Instance.Equals(wsl, ParsePath("\\\\wsl.localhost\\Ubuntu\\Home")));
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
