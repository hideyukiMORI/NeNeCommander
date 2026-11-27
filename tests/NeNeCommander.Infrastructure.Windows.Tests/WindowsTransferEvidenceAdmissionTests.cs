using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves absent recovery facts cannot become a publishable mapping or inspection result.</summary>
[TestClass]
public sealed class WindowsTransferEvidenceAdmissionTests
{
    /// <summary>Each original or preparation object is required independently before mapping admission.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenRecoveryFileObservationIsMissingRejectsItsOwningParameter()
    {
        WindowsTransferRecoveryFiles files = WindowsTransferRecoveryTestFixture.Mapping().Files;

        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryFiles.Create(null!, files.Target, files.Backup, files.Prepared), "source");
        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryFiles.Create(files.Source, null!, files.Backup, files.Prepared), "target");
        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryFiles.Create(files.Source, files.Target, null!, files.Prepared), "backup");
        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryFiles.Create(files.Source, files.Target, files.Backup, null!), "prepared");
    }

    /// <summary>Neither absent content nor absent file inventory can stand for a verified empty mapping.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenMappingEvidenceIsMissingRejectsBeforeSerialization()
    {
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping();

        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryMapping.Create(mapping.Names.Token, null!, mapping.Original, mapping.Desired), "files");
        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryMapping.Create(mapping.Names.Token, mapping.Files, null!, mapping.Desired), "original");
        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryMapping.Create(mapping.Names.Token, mapping.Files, mapping.Original, null!), "desired");
        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryJson.WriteInitial(null!), "mapping");
        TransferArgumentAssertions.Rejects(() => WindowsTransferArtifactNames.Create(null!, mapping.Names.Token), "target");
        TransferArgumentAssertions.Rejects(() => WindowsTransferArtifactNames.RequireCandidateShape(null!), "target");
    }

    /// <summary>A prepared or completed observation must retain all of its own saved frame evidence.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void CreateWhenSavedFrameEvidenceIsMissingRejectsWithoutInventingACompleteRecord()
    {
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping();
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        WindowsTransferRecoveryPrepared prepared = Assert.IsInstanceOfType<WindowsTransferRecoveryPrepared>(WindowsTransferRecoveryCodec.Inspect(initial));
        WindowsTransferRecoveryCompleted completed = Assert.IsInstanceOfType<WindowsTransferRecoveryCompleted>(WindowsTransferRecoveryCodec.Inspect(
            WindowsTransferRecoveryTestFixture.Join(initial, WindowsTransferRecoveryCodec.EncodeCompletion(initial))));

        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryInspection.Prepared(null!, prepared.Initial, prepared.IncompleteTail), "mapping");
        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryInspection.Prepared(mapping, null!, prepared.IncompleteTail), "initial");
        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryInspection.Prepared(mapping, prepared.Initial, null!), "incompleteTail");
        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryInspection.Completed(null!, completed.Initial, completed.Completion), "mapping");
        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryInspection.Completed(mapping, null!, completed.Completion), "initial");
        TransferArgumentAssertions.Rejects(() => WindowsTransferRecoveryInspection.Completed(mapping, completed.Initial, null!), "completion");
    }
}
