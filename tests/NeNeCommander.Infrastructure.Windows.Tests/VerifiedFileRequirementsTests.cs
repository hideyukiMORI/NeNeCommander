using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Requires native shape changes to stop the existing verified source-deletion route.</summary>
[TestClass]
public sealed class VerifiedFileRequirementsTests
{
    /// <summary>Unchanged ordinary identity and default bytes do not admit newly disqualifying native facts.</summary>
    [TestMethod]
    [DataRow("source", "hidden")]
    [DataRow("target", "hidden")]
    [DataRow("source", "emptyStream")]
    [DataRow("target", "emptyStream")]
    [DataRow("source", "namedStream")]
    [DataRow("target", "namedStream")]
    [DataRow("source", "hardlink")]
    [DataRow("target", "hardlink")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task DeleteWhenRequiredSourceOrTargetShapeChangesRetainsSourceAsync(string side, string defect)
    {
        using VerifiedFileRequirementsFixture fixture = await VerifiedFileRequirementsFixture.CreateAsync(
            FileVerificationRequirements.OverwriteContents, "kept", null);
        fixture.Change(fixture.Path(side), defect);
        Assert.AreEqual(fixture.Receipt.Root.Source, fixture.Access.Capture(fixture.Receipt.Root.Source.Path));
        Assert.AreEqual(fixture.Receipt.Root.Target, fixture.Access.Capture(fixture.Receipt.Root.Target.Path));

        ProviderStepOutcome outcome = VerifiedSourceDeletion.Execute(fixture.Receipt.Source, fixture.Receipt, fixture.Access);

        Assert.IsNotNull(outcome.Failure);
        Assert.IsEmpty(outcome.Receipt.Effects);
        Assert.AreEqual("kept", File.ReadAllText(fixture.Source));
        Assert.AreEqual("kept", File.ReadAllText(fixture.Target));
    }

    /// <summary>Fresh strict admission still permits the existing exact source removal for empty and populated files.</summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("kept")]
    public async Task DeleteWhenRequiredFileRemainsQualifiedRemovesOnlySourceAsync(string content)
    {
        using VerifiedFileRequirementsFixture fixture = await VerifiedFileRequirementsFixture.CreateAsync(
            FileVerificationRequirements.OverwriteContents, content, null);

        ProviderStepOutcome outcome = VerifiedSourceDeletion.Execute(fixture.Receipt.Source, fixture.Receipt, fixture.Access);

        Assert.IsNull(outcome.Failure);
        Assert.AreSame(ProviderStepCompletion.Succeeded, outcome.Completion);
        Assert.IsEmpty(outcome.Receipt.Effects);
        Assert.IsFalse(File.Exists(fixture.Source));
        Assert.AreEqual(content, File.ReadAllText(fixture.Target));
    }

    /// <summary>The ordinary copy contract continues to preserve an existing named stream through its original provider route.</summary>
    [TestMethod]
    public async Task DeleteWhenOrdinaryCopyHasPreservedNamedStreamKeepsExistingContractAsync()
    {
        using VerifiedFileRequirementsFixture fixture = await VerifiedFileRequirementsFixture.CreateAsync(
            FileVerificationRequirements.DefaultStream, "kept", "retained stream");
        Assert.AreEqual("retained stream", File.ReadAllText(fixture.Target + ":extra"));

        ProviderStepOutcome outcome = VerifiedSourceDeletion.Execute(fixture.Receipt.Source, fixture.Receipt, fixture.Access);

        Assert.AreSame(ProviderStepCompletion.Succeeded, outcome.Completion);
        Assert.IsFalse(File.Exists(fixture.Source));
        Assert.AreEqual("kept", File.ReadAllText(fixture.Target));
        Assert.AreEqual("retained stream", File.ReadAllText(fixture.Target + ":extra"));
    }
}
