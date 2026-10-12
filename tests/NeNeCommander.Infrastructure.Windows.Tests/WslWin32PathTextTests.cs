using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the one WSL Win32 path resolver of ADR-0060 produces exact extended-length text.</summary>
[TestClass]
public sealed class WslWin32PathTextTests
{
    /// <summary>
    /// Proves the resolver replaces only the ordinary UNC prefix and keeps the distribution name and
    /// every nested segment, including trailing dots, trailing spaces, and the root separator.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public void ResolveWhenPathIsValidProducesExactExtendedLengthText()
    {
        Assert.AreEqual(
            "\\\\?\\UNC\\wsl.localhost\\Ubuntu-22.04\\home\\user\\sub.\\foo.",
            WslWin32PathText.Resolve(Wsl("\\\\wsl.localhost\\Ubuntu-22.04\\home\\user\\sub.\\foo.")));
        Assert.AreEqual(
            "\\\\?\\UNC\\wsl.localhost\\Debian\\tmp\\bar \\inner",
            WslWin32PathText.Resolve(Wsl("\\\\wsl$\\Debian\\tmp\\bar \\inner")));
        Assert.AreEqual(
            "\\\\?\\UNC\\wsl.localhost\\Ubuntu\\",
            WslWin32PathText.Resolve(Wsl("\\\\wsl.localhost\\Ubuntu\\")));
    }

    /// <summary>
    /// Proves the extended-length text is a transport detail that the path model never accepts back,
    /// so it cannot leave Infrastructure as a validated path.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public void ResolveOutputIsRejectedByThePathModel()
    {
        string resolved = WslWin32PathText.Resolve(Wsl("\\\\wsl.localhost\\Ubuntu\\home\\foo."));

        _ = Assert.IsInstanceOfType<PathParseFailure>(FileSystemPath.Parse(resolved));
    }

    /// <summary>Proves a missing path is a caller defect.</summary>
    [TestMethod]
    public void ResolveWhenPathIsNullRejectsDefect()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WslWin32PathText.Resolve(null!));
    }

    private static WslPath Wsl(string text)
    {
        return Assert.IsInstanceOfType<WslPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }
}
