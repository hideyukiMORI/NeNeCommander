using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves bounded candidate derivation and immutable mapping relationships without native effects.</summary>
[TestClass]
public sealed class WindowsTransferArtifactNamesTests
{
    /// <summary>Proves one even-repeated opaque token derives exactly the same three siblings, without uniqueness claims.</summary>
    [TestMethod]
    public void CreateWhenTokenRepeatsKeepsExactSiblingNames()
    {
        WindowsLocalPath target = WindowsTransferRecoveryTestFixture.Path(@"D:\target.txt");
        Guid token = WindowsTransferRecoveryTestFixture.Token;
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(target, token);

        Assert.AreEqual(@"D:\NeNeCommander-0123456789abcdef0123456789abcdef-recovery.txt", names.Recovery.CanonicalText);
        Assert.AreEqual(@"D:\NeNeCommander-0123456789abcdef0123456789abcdef-original.bin", names.OriginalBackup.CanonicalText);
        Assert.AreEqual(@"D:\NeNeCommander-0123456789abcdef0123456789abcdef-prepared.bin", names.PreparedContent.CanonicalText);
        Assert.AreEqual(names, WindowsTransferArtifactNames.Create(target, token));
        Assert.AreEqual(target.Parent, names.Recovery.Parent);
        Assert.AreEqual(Guid.Empty, WindowsTransferArtifactNames.Create(target, Guid.Empty).Token);
    }

    /// <summary>Proves roots, unrepresentable siblings and collisions with the exact original target refuse.</summary>
    [TestMethod]
    [DataRow("root")]
    [DataRow("tooLong")]
    [DataRow("recovery")]
    [DataRow("backup")]
    [DataRow("prepared")]
    public void CreateWhenTargetCannotAdmitCandidatesRefusesIt(string defect)
    {
        WindowsTransferArtifactNames ordinary = WindowsTransferArtifactNames.Create(WindowsTransferRecoveryTestFixture.Path(@"D:\target"), Guid.Empty);
        WindowsLocalPath target = defect switch
        {
            "root" => WindowsTransferRecoveryTestFixture.Path(@"D:\"),
            "tooLong" => WindowsTransferRecoveryTestFixture.Path(@"D:\" + new string('x', 32760) + @"\a"),
            "recovery" => ordinary.Recovery,
            "backup" => ordinary.OriginalBackup,
            "prepared" => ordinary.PreparedContent,
            _ => throw new ArgumentOutOfRangeException(nameof(defect)),
        };

        _ = Assert.Throws<ArgumentException>(() => WindowsTransferArtifactNames.Create(target, Guid.Empty));
    }

    /// <summary>Proves pre-token shape admission does not mistake a dummy token collision for a malformed target.</summary>
    [TestMethod]
    public void RequireCandidateShapeWhenTargetEqualsDummyCandidateAdmitsRealDifferentToken()
    {
        WindowsLocalPath target = WindowsTransferArtifactNames.Create(WindowsTransferRecoveryTestFixture.Path(@"D:\target"), Guid.Empty).OriginalBackup;

        WindowsTransferArtifactNames.RequireCandidateShape(target);
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(target, WindowsTransferRecoveryTestFixture.Token);
        Assert.AreNotEqual(target, names.OriginalBackup);
        _ = Assert.ThrowsExactly<ArgumentException>(() => WindowsTransferArtifactNames.Create(target, Guid.Empty));
        _ = Assert.ThrowsExactly<ArgumentException>(() => WindowsTransferArtifactNames.RequireCandidateShape(WindowsTransferRecoveryTestFixture.Path(@"D:\")));
        _ = Assert.ThrowsExactly<ArgumentException>(() => WindowsTransferArtifactNames.RequireCandidateShape(WindowsTransferRecoveryTestFixture.Path(@"D:\" + new string('x', 32760) + @"\a")));
    }

    /// <summary>Proves mapping file sets reject path aliases, stable object aliases, roots and remote entries.</summary>
    [TestMethod]
    [DataRow("pathAlias")]
    [DataRow("objectAlias")]
    [DataRow("root")]
    [DataRow("unc")]
    public void CreateWhenFileSetAliasesOrLeavesLocalBoundaryRefusesIt(string defect)
    {
        WindowsTransferRecoveryFiles files = WindowsTransferRecoveryTestFixture.Mapping().Files;
        VerifiedEntrySnapshot source = defect switch
        {
            "pathAlias" => WindowsTransferRecoveryTestFixture.Entry(WindowsTransferRecoveryTestFixture.Path(files.Target.Path.CanonicalText.ToUpperInvariant()), "other"),
            "objectAlias" => VerifiedEntrySnapshot.Create(files.Source.Path, files.Source.Identity, files.Target.ObjectIdentity),
            "root" => WindowsTransferRecoveryTestFixture.Entry(WindowsTransferRecoveryTestFixture.Path(@"D:\"), "other"),
            "unc" => WindowsTransferRecoveryTestFixture.Entry(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(@"\\host\share\source")).Path, "other"),
            _ => throw new ArgumentOutOfRangeException(nameof(defect)),
        };

        _ = Assert.ThrowsExactly<ArgumentException>(() => WindowsTransferRecoveryFiles.Create(source, files.Target, files.Backup, files.Prepared));
    }

    /// <summary>Proves target-relative names, roles, casing and recovery-record/source separation remain exact.</summary>
    [TestMethod]
    [DataRow("backup")]
    [DataRow("prepared")]
    [DataRow("swapped")]
    [DataRow("case")]
    [DataRow("sourceRecovery")]
    [DataRow("token")]
    public void CreateWhenMappingNamesDoNotMatchExactTokenAndTargetRefusesIt(string defect)
    {
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping();
        WindowsTransferRecoveryFiles files = mapping.Files;
        VerifiedEntrySnapshot source = defect == "sourceRecovery" ? WindowsTransferRecoveryTestFixture.Entry(mapping.Names.Recovery, "source") : files.Source;
        VerifiedEntrySnapshot backup = defect switch
        {
            "backup" => WindowsTransferRecoveryTestFixture.Entry(WindowsTransferRecoveryTestFixture.Path(@"D:\elsewhere\backup"), "backup"),
            "swapped" => files.Prepared,
            "case" => WindowsTransferRecoveryTestFixture.Entry(WindowsTransferRecoveryTestFixture.Path(mapping.Names.OriginalBackup.CanonicalText.ToUpperInvariant()), "backup"),
            _ => files.Backup,
        };
        VerifiedEntrySnapshot prepared = defect == "swapped" ? files.Backup : defect == "prepared"
            ? WindowsTransferRecoveryTestFixture.Entry(WindowsTransferRecoveryTestFixture.Path(@"D:\elsewhere\prepared"), "prepared") : files.Prepared;
        WindowsTransferRecoveryFiles changed = WindowsTransferRecoveryFiles.Create(source, files.Target, backup, prepared);
        Guid token = defect == "token" ? Guid.Empty : mapping.Names.Token;

        _ = Assert.ThrowsExactly<ArgumentException>(() => WindowsTransferRecoveryMapping.Create(token, changed, mapping.Original, mapping.Desired));
    }
}
