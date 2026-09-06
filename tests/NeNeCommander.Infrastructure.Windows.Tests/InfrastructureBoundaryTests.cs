using System;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Diagnostics;
using NeNeCommander.Infrastructure.Windows.FileOperations;
using NeNeCommander.Infrastructure.Windows.Paths;
using NeNeCommander.Infrastructure.Windows.Settings;
using NeNeCommander.Infrastructure.Windows.Time;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves deterministic Windows adapter-boundary defenses.</summary>
[TestClass]
public sealed class InfrastructureBoundaryTests
{
    /// <summary>Proves containment uses provider and segment identity rather than string prefixes.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public void EvaluateWhenCandidateResolvesOutsideOperationRootRejectsCandidate()
    {
        FileSystemPath root = ParsePath("C:\\operation-root");
        FileSystemPath child = ParsePath("C:\\operation-root\\child");
        FileSystemPath prefixCollision = ParsePath("C:\\operation-rooted\\child");
        FileSystemPath otherProvider = ParsePath("D:\\operation-root\\child");

        Assert.AreSame(child, Assert.IsInstanceOfType<ContainedPath>(
            ProviderPathContainment.Evaluate(root, child)).Path);
        Assert.AreSame(
            PathContainmentFailureKind.OutsideRoot,
            Assert.IsInstanceOfType<RejectedPathContainment>(
                ProviderPathContainment.Evaluate(root, prefixCollision)).Kind);
        Assert.AreSame(
            PathContainmentFailureKind.ProviderMismatch,
            Assert.IsInstanceOfType<RejectedPathContainment>(
                ProviderPathContainment.Evaluate(root, otherProvider)).Kind);
    }

    /// <summary>Proves WSL containment preserves Linux segment casing.</summary>
    [TestMethod]
    public void EvaluateWhenWslSegmentCaseDiffersRejectsCandidate()
    {
        FileSystemPath root = ParsePath("\\\\wsl.localhost\\Ubuntu\\home\\xi");
        FileSystemPath child = ParsePath("\\\\wsl.localhost\\ubuntu\\home\\xi\\Case");
        FileSystemPath caseMismatch = ParsePath("\\\\wsl.localhost\\Ubuntu\\home\\XI\\Case");
        FileSystemPath prefixCollision = ParsePath("\\\\wsl.localhost\\Ubuntu\\home\\xian\\Case");
        FileSystemPath distroMismatch = ParsePath("\\\\wsl.localhost\\Debian\\home\\xi\\Case");

        _ = Assert.IsInstanceOfType<ContainedPath>(ProviderPathContainment.Evaluate(root, child));
        Assert.AreSame(
            PathContainmentFailureKind.OutsideRoot,
            Assert.IsInstanceOfType<RejectedPathContainment>(
                ProviderPathContainment.Evaluate(root, caseMismatch)).Kind);
        Assert.AreSame(
            PathContainmentFailureKind.OutsideRoot,
            Assert.IsInstanceOfType<RejectedPathContainment>(
                ProviderPathContainment.Evaluate(root, prefixCollision)).Kind);
        Assert.AreSame(
            PathContainmentFailureKind.ProviderMismatch,
            Assert.IsInstanceOfType<RejectedPathContainment>(
                ProviderPathContainment.Evaluate(root, distroMismatch)).Kind);
    }

    /// <summary>Proves UNC identity checks include server, share, root equality, and segment boundaries.</summary>
    [TestMethod]
    public void EvaluateWhenUncCandidatesVaryAppliesExactProviderAndContainmentRules()
    {
        FileSystemPath root = ParsePath("\\\\server\\share\\root");
        FileSystemPath exact = ParsePath("\\\\SERVER\\SHARE\\ROOT");
        FileSystemPath child = ParsePath("\\\\server\\share\\root\\child");
        FileSystemPath prefixCollision = ParsePath("\\\\server\\share\\rooted");
        FileSystemPath otherServer = ParsePath("\\\\other\\share\\root");
        FileSystemPath otherShare = ParsePath("\\\\server\\other\\root");

        _ = Assert.IsInstanceOfType<ContainedPath>(ProviderPathContainment.Evaluate(root, exact));
        _ = Assert.IsInstanceOfType<ContainedPath>(ProviderPathContainment.Evaluate(root, child));
        AssertContainmentFailure(root, prefixCollision, PathContainmentFailureKind.OutsideRoot);
        AssertContainmentFailure(root, otherServer, PathContainmentFailureKind.ProviderMismatch);
        AssertContainmentFailure(root, otherShare, PathContainmentFailureKind.ProviderMismatch);
    }

    /// <summary>Proves provider roots and provider-type mismatches take their exact branches.</summary>
    [TestMethod]
    public void EvaluateWhenProviderRootsOrTypesDifferAppliesExactBoundaryRules()
    {
        FileSystemPath localRoot = ParsePath("C:\\");
        FileSystemPath localChild = ParsePath("c:\\child");
        FileSystemPath wslRoot = ParsePath("\\\\wsl.localhost\\Ubuntu");
        FileSystemPath wslExact = ParsePath("\\\\wsl.localhost\\ubuntu");
        FileSystemPath wslChild = ParsePath("\\\\wsl.localhost\\Ubuntu\\home");
        FileSystemPath unc = ParsePath("\\\\server\\share");

        _ = Assert.IsInstanceOfType<ContainedPath>(ProviderPathContainment.Evaluate(localRoot, localChild));
        _ = Assert.IsInstanceOfType<ContainedPath>(ProviderPathContainment.Evaluate(wslRoot, wslExact));
        _ = Assert.IsInstanceOfType<ContainedPath>(ProviderPathContainment.Evaluate(wslRoot, wslChild));
        AssertContainmentFailure(localRoot, unc, PathContainmentFailureKind.ProviderMismatch);
    }

    /// <summary>Proves hostile distribution text cannot become a provider path.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-009")]
    public void ParseWhenDistributionNameContainsCommandSyntaxRejectsInput()
    {
        PathParseOutcome outcome = FileSystemPath.Parse(
            "\\\\wsl.localhost\\Ubuntu;Remove-Item\\home\\xi");

        Assert.AreSame(
            PathParseFailureKind.InvalidDistribution,
            Assert.IsInstanceOfType<PathParseFailure>(outcome).Kind);
    }

    /// <summary>Proves diagnostics expose only stable salted fixed-length identifiers.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-010")]
    public void CreateWhenPathIsSensitiveEmitsOnlySaltedFingerprint()
    {
        FileSystemPath path = ParsePath("\\\\private-server\\finance\\payroll.xlsx");
        DiagnosticSalt firstSalt = ParseSalt("0123456789abcdef0123456789abcdef");
        DiagnosticSalt secondSalt = ParseSalt("abcdef0123456789abcdef0123456789");

        DiagnosticPathFingerprint first = DiagnosticPathFingerprint.Create(path, firstSalt);
        DiagnosticPathFingerprint repeated = DiagnosticPathFingerprint.Create(path, firstSalt);
        DiagnosticPathFingerprint changedSalt = DiagnosticPathFingerprint.Create(path, secondSalt);

        Assert.AreEqual(first, repeated);
        Assert.AreNotEqual(first, changedSalt);
        Assert.HasCount(16, first.Value);
        Assert.DoesNotContain("private-server", first.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payroll", first.Value, StringComparison.OrdinalIgnoreCase);
        _ = Assert.IsInstanceOfType<DiagnosticSaltRejected>(DiagnosticSalt.Parse("too-short"));
        _ = Assert.IsInstanceOfType<DiagnosticSaltAccepted>(DiagnosticSalt.Parse(new string('s', 256)));
    }

    /// <summary>Proves malformed and incomplete settings never become partial accepted state.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public void ValidateWhenSettingsAreHostileReturnsExactTypedRejections()
    {
        AssertSettingsFailure(null, SettingsReadFailureKind.Empty);
        AssertSettingsFailure("   ", SettingsReadFailureKind.Empty);
        AssertSettingsFailure(new string('x', 65537), SettingsReadFailureKind.TooLarge);
        AssertSettingsFailure(new string('x', 65536), SettingsReadFailureKind.Malformed);
        AssertSettingsFailure("{", SettingsReadFailureKind.Malformed);
        AssertSettingsFailure("[]", SettingsReadFailureKind.Malformed);
        AssertSettingsFailure(
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-",
            SettingsReadFailureKind.Malformed);
        AssertSettingsFailure(
                                 /*lang=json,strict*/
                                 "{\"schemaVersion\":3,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}",
            SettingsReadFailureKind.UnknownVersion);
        AssertSettingsFailure(
                                 /*lang=json,strict*/
                                 "{\"schemaVersion\":99999999999,\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}",
            SettingsReadFailureKind.UnknownVersion);
        AssertSettingsFailure(
                                 /*lang=json,strict*/
                                 "{\"schemaVersion\":\"1\",\"showHiddenItems\":false,\"colorScheme\":\"nene-dark\"}",
            SettingsReadFailureKind.Malformed);
        AssertSettingsFailure(
                                 /*lang=json,strict*/
                                 "{\"schemaVersion\":1,\"unexpected\":true,\"showHiddenItems\":true,\"colorScheme\":\"nene-dark\"}",
            SettingsReadFailureKind.UnexpectedProperty);
        AssertSettingsFailure(
                                 /*lang=json,strict*/
                                 "{\"schemaVersion\":1,\"showHiddenItems\":true}",
            SettingsReadFailureKind.Incomplete);
        AssertSettingsFailure(
                                 /*lang=json,strict*/
                                 "{\"schemaVersion\":1,\"showHiddenItems\":null,\"colorScheme\":\"nene-dark\"}",
            SettingsReadFailureKind.Malformed);
        AssertSettingsFailure(
                                 /*lang=json,strict*/
                                 "{\"schemaVersion\":1,\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"nene-dark\"}",
            SettingsReadFailureKind.UnexpectedProperty);
        AssertSettingsFailure(
                                 /*lang=json,strict*/
                                 "{\"schemaVersion\":1,\"schemaVersion\":1,\"showHiddenItems\":true}",
            SettingsReadFailureKind.Incomplete);
        AssertSettingsFailure(
                                 /*lang=json,strict*/
                                 "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"../nene-dark\"}",
            SettingsReadFailureKind.UnknownColorScheme);
        AssertSettingsFailure(
                                 /*lang=json,strict*/
                                 "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":7}",
            SettingsReadFailureKind.Malformed);
    }

    /// <summary>Proves only a complete current settings schema becomes typed settings.</summary>
    [TestMethod]
    public void ValidateWhenSettingsAreCompleteReturnsTypedSettings()
    {
        SettingsReadOutcome shown = SettingsDocumentValidator.Validate(
                                 /*lang=json,strict*/
                                 "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"dracula\"}");
        SettingsReadOutcome hidden = SettingsDocumentValidator.Validate(
                                 /*lang=json,strict*/
                                 "{\"colorScheme\":\"solarized-light\",\"showHiddenItems\":false,\"schemaVersion\":1}");

        UserSettings shownSettings = Assert.IsInstanceOfType<SettingsRead>(shown).Settings;
        Assert.AreSame(ColorScheme.Dracula, shownSettings.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, shownSettings.HiddenItemVisibility);
        UserSettings hiddenSettings = Assert.IsInstanceOfType<SettingsRead>(hidden).Settings;
        Assert.AreSame(ColorScheme.SolarizedLight, hiddenSettings.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Hidden, hiddenSettings.HiddenItemVisibility);
    }

    /// <summary>Proves Windows failures normalize without retry or permissive fallback.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    public void NormalizeWhenAvailabilityOrPermissionChangesReturnsFailClosedOutcome()
    {
        Assert.AreSame(FileOperationFailureKind.AccessDenied, WindowsFileFailureNormalizer.Normalize(
            unchecked((int)0x80070005)));
        Assert.AreSame(FileOperationFailureKind.NotFound, WindowsFileFailureNormalizer.Normalize(
            unchecked((int)0x80070002)));
        Assert.AreSame(FileOperationFailureKind.NotFound, WindowsFileFailureNormalizer.Normalize(
            unchecked((int)0x80070003)));
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, WindowsFileFailureNormalizer.Normalize(
            unchecked((int)0x80070035)));
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, WindowsFileFailureNormalizer.Normalize(
            unchecked((int)0x80070043)));
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, WindowsFileFailureNormalizer.Normalize(
            unchecked((int)0x81234567)));
    }

    /// <summary>Proves the production clock advances monotonically.</summary>
    [TestMethod]
    public void GetMonotonicTimeWhenCalledRepeatedlyDoesNotMoveBackward()
    {
        StopwatchClock clock = new();

        TimeSpan first = clock.GetMonotonicTime();
        TimeSpan second = clock.GetMonotonicTime();

        Assert.IsGreaterThanOrEqualTo(first, second);
    }

    private static void AssertSettingsFailure(string? input, SettingsReadFailureKind expected)
    {
        SettingsRejected rejected = Assert.IsInstanceOfType<SettingsRejected>(
            SettingsDocumentValidator.Validate(input));
        Assert.AreSame(expected, rejected.Kind);
    }

    private static DiagnosticSalt ParseSalt(string input)
    {
        return Assert.IsInstanceOfType<DiagnosticSaltAccepted>(DiagnosticSalt.Parse(input)).Salt;
    }

    private static void AssertContainmentFailure(
        FileSystemPath root,
        FileSystemPath candidate,
        PathContainmentFailureKind expected)
    {
        RejectedPathContainment rejected = Assert.IsInstanceOfType<RejectedPathContainment>(
            ProviderPathContainment.Evaluate(root, candidate));
        Assert.AreSame(expected, rejected.Kind);
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
