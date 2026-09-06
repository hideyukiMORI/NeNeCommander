using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Domain.Tests;

/// <summary>Proves the canonical filesystem path parser contract.</summary>
[TestClass]
public sealed class FileSystemPathTests
{
    private const int MaximumPathLength = 32767;
    private const int LegacyWslAliasExpansionLength = 9;

    /// <summary>Proves local path canonicalization.</summary>
    [TestMethod]
    public void ParseWhenWindowsLocalPathContainsRedundantSegmentsCanonicalLocalPath()
    {
        PathParseSuccess success = RequireSuccess(FileSystemPath.Parse("c:/work/./source/../target"));

        WindowsLocalPath path = Assert.IsInstanceOfType<WindowsLocalPath>(success.Path);
        Assert.AreEqual("C:\\work\\target", path.CanonicalText);
        Assert.AreEqual("C:", path.Drive);
    }

    /// <summary>Proves local root preservation.</summary>
    [TestMethod]
    public void ParseWhenWindowsLocalRootProvidedCanonicalRoot()
    {
        PathParseSuccess success = RequireSuccess(FileSystemPath.Parse("D:\\"));

        Assert.AreEqual("D:\\", success.Path.CanonicalText);
    }

    /// <summary>Proves UNC path canonicalization.</summary>
    [TestMethod]
    public void ParseWhenUncPathProvidedCanonicalUncPath()
    {
        PathParseSuccess success = RequireSuccess(FileSystemPath.Parse("\\\\Server\\Share\\one\\\\two"));

        WindowsUncPath path = Assert.IsInstanceOfType<WindowsUncPath>(success.Path);
        Assert.AreEqual("\\\\Server\\Share\\one\\two", path.CanonicalText);
        Assert.AreEqual("Server", path.Server);
        Assert.AreEqual("Share", path.Share);
    }

    /// <summary>Proves UNC and WSL provider roots remain canonical roots.</summary>
    [TestMethod]
    public void ParseWhenProviderRootProvidedCanonicalProviderRoot()
    {
        WindowsUncPath unc = Assert.IsInstanceOfType<WindowsUncPath>(
            RequireSuccess(FileSystemPath.Parse("\\\\Server\\Share")).Path);
        WslPath wsl = Assert.IsInstanceOfType<WslPath>(
            RequireSuccess(FileSystemPath.Parse("\\\\wsl.localhost\\Ubuntu")).Path);

        Assert.AreEqual("\\\\Server\\Share\\", unc.CanonicalText);
        Assert.AreEqual("\\\\wsl.localhost\\Ubuntu\\", wsl.CanonicalText);
        Assert.AreEqual("/", wsl.LinuxPath);
    }

    /// <summary>Proves WSL alias and case-preserving identity behavior.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-002")]
    public void ParseWhenWslAliasAndMixedCasePathProvidedCanonicalAliasPreservingCase()
    {
        PathParseSuccess legacy = RequireSuccess(
            FileSystemPath.Parse("\\\\wsl$\\Ubuntu-22.04\\home\\xi\\CaseName"));
        PathParseSuccess current = RequireSuccess(
            FileSystemPath.Parse("\\\\WSL.LOCALHOST\\Ubuntu-22.04\\home\\xi\\CaseName"));

        WslPath legacyPath = Assert.IsInstanceOfType<WslPath>(legacy.Path);
        WslPath currentPath = Assert.IsInstanceOfType<WslPath>(current.Path);
        Assert.AreEqual("\\\\wsl.localhost\\Ubuntu-22.04\\home\\xi\\CaseName", legacyPath.CanonicalText);
        Assert.AreEqual(legacyPath, currentPath);
        Assert.AreEqual("Ubuntu-22.04", legacyPath.DistributionName);
        Assert.AreEqual("/home/xi/CaseName", legacyPath.LinuxPath);
    }

    /// <summary>Proves provider roots cannot be escaped through parent traversal.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-001")]
    public void ParseWhenParentTraversalCrossesRootParentTraversalFailure()
    {
        PathParseFailure local = RequireFailure(FileSystemPath.Parse("C:\\..\\escape"));
        PathParseFailure unc = RequireFailure(FileSystemPath.Parse("\\\\server\\share\\..\\escape"));
        PathParseFailure wsl = RequireFailure(FileSystemPath.Parse("\\\\wsl.localhost\\Ubuntu\\..\\escape"));

        Assert.AreSame(PathParseFailureKind.ParentTraversal, local.Kind);
        Assert.AreSame(PathParseFailureKind.ParentTraversal, unc.Kind);
        Assert.AreSame(PathParseFailureKind.ParentTraversal, wsl.Kind);
    }

    /// <summary>Proves ambiguous and illegal path forms have typed rejections.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    [DataRow("relative\\path", "Relative")]
    [DataRow("C:relative", "Relative")]
    [DataRow("1:\\path", "Relative")]
    [DataRow("C;\\path", "Relative")]
    [DataRow("\\\\server", "InvalidRoot")]
    [DataRow("\\\\server\\bad|share", "InvalidRoot")]
    [DataRow("\\\\server\\.", "InvalidRoot")]
    [DataRow("\\\\server\\..", "InvalidRoot")]
    [DataRow("\\\\server \\share", "InvalidRoot")]
    [DataRow("\\\\server\\share.", "InvalidRoot")]
    [DataRow("\\\\server\\share ", "InvalidRoot")]
    [DataRow("C:\\NUL.txt", "InvalidSegment")]
    [DataRow("C:\\CON", "InvalidSegment")]
    [DataRow("C:\\PRN", "InvalidSegment")]
    [DataRow("C:\\AUX", "InvalidSegment")]
    [DataRow("C:\\COM1", "InvalidSegment")]
    [DataRow("C:\\LPT9", "InvalidSegment")]
    [DataRow("C:\\bad?.txt", "InvalidSegment")]
    [DataRow("C:\\trailing. ", "InvalidSegment")]
    [DataRow("C:\\trailing ", "InvalidSegment")]
    [DataRow("C:\\trailing.", "InvalidSegment")]
    [DataRow("\\\\?\\C:\\unsafe", "DeviceNamespace")]
    [DataRow("\\\\.\\PhysicalDrive0", "DeviceNamespace")]
    [DataRow("\\\\wsl.localhost\\-unsafe\\home", "InvalidDistribution")]
    [DataRow("\\\\wsl.localhost\\Ubuntu;shutdown\\home", "InvalidDistribution")]
    public void ParseWhenPathIsAmbiguousOrIllegalTypedFailure(string input, string expectedKind)
    {
        PathParseFailure failure = RequireFailure(FileSystemPath.Parse(input));

        Assert.AreEqual(expectedKind, failure.Kind.GetType().Name.Replace("Failure", string.Empty));
    }

    /// <summary>Proves names outside the exact Windows device-name set remain usable.</summary>
    [TestMethod]
    [DataRow("C:\\COM0")]
    [DataRow("C:\\LPT0")]
    [DataRow("C:\\ABCD")]
    [DataRow("C:\\ABC1")]
    [DataRow("C:\\ leading")]
    [DataRow("C:\\.leading")]
    public void ParseWhenSegmentResemblesDeviceNameButIsValidAcceptsPath(string input)
    {
        _ = RequireSuccess(FileSystemPath.Parse(input));
    }

    /// <summary>Proves missing input is rejected.</summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ParseWhenInputIsMissingEmptyFailure(string? input)
    {
        PathParseFailure failure = RequireFailure(FileSystemPath.Parse(input));

        Assert.AreSame(PathParseFailureKind.Empty, failure.Kind);
    }

    /// <summary>Proves the parser accepts a valid path at its exact fixed size boundary.</summary>
    [TestMethod]
    public void ParseWhenInputMeetsBoundaryAcceptsPath()
    {
        string input = CreateWindowsLocalPath(MaximumPathLength);

        PathParseSuccess success = RequireSuccess(FileSystemPath.Parse(input));

        Assert.AreEqual(MaximumPathLength, success.Path.CanonicalText.Length);
    }

    /// <summary>Proves the parser rejects the first length beyond its fixed size boundary.</summary>
    [TestMethod]
    public void ParseWhenInputExceedsBoundaryTooLongFailure()
    {
        string input = CreateWindowsLocalPath(MaximumPathLength + 1);

        PathParseFailure failure = RequireFailure(FileSystemPath.Parse(input));

        Assert.AreSame(PathParseFailureKind.TooLong, failure.Kind);
    }

    /// <summary>Proves an exact-boundary UNC canonical path is accepted and remains closed under parsing.</summary>
    [TestMethod]
    public void ParseWhenUncCanonicalTextMeetsBoundaryAcceptsAndReparses()
    {
        string input = CreateUncRootWithCanonicalLength(MaximumPathLength);

        PathParseSuccess success = RequireSuccess(FileSystemPath.Parse(input));
        PathParseSuccess reparsed = RequireSuccess(FileSystemPath.Parse(success.Path.CanonicalText));

        Assert.AreEqual(MaximumPathLength, success.Path.CanonicalText.Length);
        Assert.AreEqual(success.Path.CanonicalText, reparsed.Path.CanonicalText);
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(success.Path, reparsed.Path));
    }

    /// <summary>Proves a UNC root separator cannot expand canonical text past the fixed boundary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public void ParseWhenUncCanonicalTextExceedsBoundaryTooLongFailure()
    {
        string input = CreateUncRootWithCanonicalLength(MaximumPathLength + 1);

        PathParseFailure failure = RequireFailure(FileSystemPath.Parse(input));

        Assert.AreSame(PathParseFailureKind.TooLong, failure.Kind);
    }

    /// <summary>Proves an exact-boundary WSL alias canonical path is accepted and remains closed under parsing.</summary>
    [TestMethod]
    public void ParseWhenWslAliasCanonicalTextMeetsBoundaryAcceptsAndReparses()
    {
        string input = CreateWslAliasPathWithCanonicalLength(MaximumPathLength);

        PathParseSuccess success = RequireSuccess(FileSystemPath.Parse(input));
        PathParseSuccess reparsed = RequireSuccess(FileSystemPath.Parse(success.Path.CanonicalText));

        Assert.AreEqual(MaximumPathLength, success.Path.CanonicalText.Length);
        Assert.AreEqual(success.Path.CanonicalText, reparsed.Path.CanonicalText);
        Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(success.Path, reparsed.Path));
    }

    /// <summary>Proves WSL alias expansion cannot produce canonical text past the fixed boundary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-002")]
    public void ParseWhenWslAliasCanonicalTextExceedsBoundaryTooLongFailure()
    {
        string input = CreateWslAliasPathWithCanonicalLength(MaximumPathLength + 1);

        PathParseFailure failure = RequireFailure(FileSystemPath.Parse(input));

        Assert.AreSame(PathParseFailureKind.TooLong, failure.Kind);
    }

    /// <summary>Proves maximum-length raw WSL alias text is rejected when its canonical form expands past the boundary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-002")]
    public void ParseWhenMaximumRawWslAliasExpandsPastCanonicalBoundaryTooLongFailure()
    {
        string input = CreateWslAliasPathWithRawLength(MaximumPathLength);

        PathParseFailure failure = RequireFailure(FileSystemPath.Parse(input));

        Assert.AreEqual(MaximumPathLength, input.Length);
        Assert.AreEqual(
            MaximumPathLength + LegacyWslAliasExpansionLength,
            input.Replace("\\\\wsl$\\", "\\\\wsl.localhost\\").Length);
        Assert.AreSame(PathParseFailureKind.TooLong, failure.Kind);
    }

    /// <summary>Proves control characters cannot cross the path boundary.</summary>
    [TestMethod]
    public void ParseWhenInputContainsControlCharacterInvalidSegmentFailure()
    {
        PathParseFailure failure = RequireFailure(FileSystemPath.Parse("C:\\safe\u0001unsafe"));

        Assert.AreSame(PathParseFailureKind.InvalidSegment, failure.Kind);
    }

    private static PathParseSuccess RequireSuccess(PathParseOutcome outcome)
    {
        return outcome as PathParseSuccess ??
            throw new AssertFailedException($"Expected {nameof(PathParseSuccess)}, received {outcome.GetType().Name}.");
    }

    private static string CreateWindowsLocalPath(int totalLength)
    {
        const string root = "C:\\";
        int remainderLength = totalLength - root.Length;
        int pairCount = (remainderLength - 1) / 2;
        return root + string.Concat(Enumerable.Repeat("a\\", pairCount)) +
            new string('a', remainderLength - (pairCount * 2));
    }

    private static string CreateUncRootWithCanonicalLength(int canonicalLength)
    {
        const string prefix = "\\\\s\\";
        return prefix + new string('a', canonicalLength - prefix.Length - 1);
    }

    private static string CreateWslAliasPathWithCanonicalLength(int canonicalLength)
    {
        return CreateWslAliasPathWithRawLength(canonicalLength - LegacyWslAliasExpansionLength);
    }

    private static string CreateWslAliasPathWithRawLength(int rawLength)
    {
        const string prefix = "\\\\wsl$\\U\\";
        return prefix + new string('a', rawLength - prefix.Length);
    }

    private static PathParseFailure RequireFailure(PathParseOutcome outcome)
    {
        return outcome as PathParseFailure ??
            throw new AssertFailedException($"Expected {nameof(PathParseFailure)}, received {outcome.GetType().Name}.");
    }
}
