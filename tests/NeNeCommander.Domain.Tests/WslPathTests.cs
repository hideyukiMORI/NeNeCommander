using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Domain.Tests;

/// <summary>Proves WSL name compatibility without changing canonical path identity.</summary>
[TestClass]
public sealed class WslPathTests
{
    /// <summary>Proves all segments use the existing Windows-name predicate, including ancestors.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-019")]
    [DataRow("", "Compatible")]
    [DataRow("home/user/Mixed Case&$(name);文.txt", "Compatible")]
    [DataRow("home/.hidden", "Compatible")]
    [DataRow("home/a..b", "Compatible")]
    [DataRow("home/name.", "Incompatible")]
    [DataRow("home/name ", "Incompatible")]
    [DataRow("home./file.txt", "Incompatible")]
    [DataRow("home /file.txt", "Incompatible")]
    [DataRow("home/CON.txt", "Incompatible")]
    [DataRow("home/nul/file.txt", "Incompatible")]
    [DataRow("home/LPT1", "Incompatible")]
    [DataRow("home/COM9.bin", "Incompatible")]
    [DataRow("home/a:b.txt", "Incompatible")]
    [DataRow("home/a?b.txt", "Incompatible")]
    [DataRow("home/a*b.txt", "Incompatible")]
    [DataRow("home/a\"b.txt", "Incompatible")]
    [DataRow("home/a<b.txt", "Incompatible")]
    [DataRow("home/a>b.txt", "Incompatible")]
    [DataRow("home/a|b.txt", "Incompatible")]
    public void HasWindowsCompatibleNamesWhenPathIsClassifiedPreservesWslText(string relative, string expected)
    {
        ArgumentNullException.ThrowIfNull(relative);
        string original = "\\\\wsl.localhost\\Ubuntu\\" + relative.Replace('/', '\\');
        PathParseSuccess parsed = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(original));
        WslPath path = Assert.IsInstanceOfType<WslPath>(parsed.Path);

        bool compatible = path.HasWindowsCompatibleNames;

        Assert.AreEqual(expected, compatible ? "Compatible" : "Incompatible");
        Assert.AreEqual(original, path.CanonicalText);
        Assert.AreEqual("/" + relative, path.LinuxPath);
    }
}
