using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the strict file requirement rejects foreign paths, aliases, stale observations and late native changes.</summary>
[TestClass]
public sealed class VerifiedFileRequirementBoundaryTests
{
    /// <summary>Typed provider exclusions and full-identity mismatches cannot acquire native deletion permission.</summary>
    [TestMethod]
    [DataRow("sourceProvider")]
    [DataRow("targetProvider")]
    [DataRow("sourceIdentity")]
    [DataRow("targetIdentity")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task MatchesRequirementsWhenEvidenceContradictsProviderOrObjectRefusesAsync(string defect)
    {
        using VerifiedFileRequirementsFixture fixture = await VerifiedFileRequirementsFixture.CreateAsync(
            FileVerificationRequirements.OverwriteContents, "kept", null);
        VerifiedCopyEntry.VerifiedFile original = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedFile>(fixture.Receipt.Root);
        VerifiedEntrySnapshot source = ChangeObservation(original.Source, defect, "source");
        VerifiedEntrySnapshot target = ChangeObservation(original.Target, defect, "target");
        VerifiedCopyEntry.VerifiedFile changed = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedFile>(
            VerifiedCopyEntry.File(source, target, original.Content, original.Requirements));

        Assert.IsFalse(fixture.Access.MatchesRequirements(changed));

        Assert.AreEqual("kept", File.ReadAllText(fixture.Source));
        Assert.AreEqual("kept", File.ReadAllText(fixture.Target));
    }

    /// <summary>The same real single-link file cannot serve as both sides even when every native shape and full identity is valid.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task MatchesRequirementsWhenBothSidesAreSameQualifiedFileRefusesAsync()
    {
        using VerifiedFileRequirementsFixture fixture = await VerifiedFileRequirementsFixture.CreateAsync(
            FileVerificationRequirements.OverwriteContents, "kept", null);
        VerifiedCopyEntry.VerifiedFile original = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedFile>(fixture.Receipt.Root);
        VerifiedCopyEntry.VerifiedFile alias = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedFile>(
            VerifiedCopyEntry.File(original.Source, original.Source, original.Content, original.Requirements));

        Assert.IsFalse(fixture.Access.MatchesRequirements(alias));

        Assert.AreEqual(original.Source, fixture.Access.Capture(original.Source.Path));
        Assert.AreEqual("kept", File.ReadAllText(fixture.Source));
        Assert.AreEqual("kept", File.ReadAllText(fixture.Target));
    }

    /// <summary>A source change during target observation requires final source admission while ordinary identity remains equal.</summary>
    [TestMethod]
    [DataRow("hidden")]
    [DataRow("emptyStream")]
    [DataRow("hardlink")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task MatchesRequirementsWhenTargetObservationChangesSourceRefusesAsync(string defect)
    {
        using VerifiedFileRequirementsFixture fixture = await VerifiedFileRequirementsFixture.CreateAsync(
            FileVerificationRequirements.OverwriteContents, "kept", null);
        VerifiedCopyEntry.VerifiedFile file = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedFile>(fixture.Receipt.Root);
        int targetQueries = 0;
        bool changed = false;
        WindowsDriveBoundary boundary = new(path =>
        {
            // Two target queries open/revalidate its lease; the third begins its observation after the first source admission.
            if (path == file.Target.Path && ++targetQueries == 3)
            {
                fixture.Change(fixture.Receipt.Root.Source.Path, defect);
                changed = true;
            }
            return DriveType.Fixed;
        });
        VerifiedTreeAccess access = new(new WindowsWslFileSystem(), boundary);

        _ = Assert.Throws<IOException>(() => access.MatchesRequirements(file));

        Assert.IsTrue(changed);
        Assert.AreEqual(file.Source, fixture.Access.Capture(file.Source.Path));
        Assert.AreEqual("kept", File.ReadAllText(fixture.Source));
        Assert.AreEqual("kept", File.ReadAllText(fixture.Target));
    }

    /// <summary>A change during the final leaf identity reads must be caught after all earlier content/shape checks passed.</summary>
    [TestMethod]
    [DataRow("source", "hidden")]
    [DataRow("target", "hidden")]
    [DataRow("source", "emptyStream")]
    [DataRow("target", "emptyStream")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task DeleteWhenFinalCaptureChangesRequiredShapeRetainsSourceAsync(string side, string defect)
    {
        using VerifiedFileRequirementsFixture fixture = await VerifiedFileRequirementsFixture.CreateAsync(
            FileVerificationRequirements.OverwriteContents, "kept", null);
        FileSystemPath affected = fixture.Path(side);
        int queries = 0;
        bool changed = false;
        WindowsDriveBoundary boundary = new(path =>
        {
            // This ordinal reaches the final Capture after both the initial and leaf byte/shape checks; causal guard-removal proof fixes the phase.
            if (path == affected && ++queries == (side == "target" ? 16 : 20))
            {
                fixture.Change(fixture.Path(side), defect);
                changed = true;
            }
            return DriveType.Fixed;
        });
        VerifiedTreeAccess access = new(new WindowsWslFileSystem(), boundary);

        ProviderStepOutcome outcome = VerifiedSourceDeletion.Execute(fixture.Receipt.Source, fixture.Receipt, access);

        Assert.IsTrue(changed);
        Assert.IsNotNull(outcome.Failure);
        Assert.IsEmpty(outcome.Receipt.Effects);
        Assert.AreEqual(fixture.Receipt.Root.Source, fixture.Access.Capture(fixture.Receipt.Root.Source.Path));
        Assert.AreEqual(fixture.Receipt.Root.Target, fixture.Access.Capture(fixture.Receipt.Root.Target.Path));
        Assert.AreEqual("kept", File.ReadAllText(fixture.Source));
        Assert.AreEqual("kept", File.ReadAllText(fixture.Target));
    }

    private static VerifiedEntrySnapshot ChangeObservation(
        VerifiedEntrySnapshot original, string defect, string side)
    {
        FileSystemPath path = defect == side + "Provider"
            ? Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(@"\\wsl.localhost\Ubuntu\owned\file.txt")).Path
            : original.Path;
        FileIdentity fullIdentity = defect == side + "Identity"
            ? Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("another-full-identity")).Identity
            : original.Identity;
        return VerifiedEntrySnapshot.Create(path, fullIdentity, original.ObjectIdentity);
    }
}
