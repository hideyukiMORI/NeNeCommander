using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves deletion requirements remain in the single immutable descendant evidence.</summary>
[TestClass]
public sealed class VerifiedFileRequirementsTests
{
    /// <summary>Requirements survive file and descendant construction without becoming another receipt.</summary>
    [TestMethod]
    public void CreateWhenStrictFileIsNestedRetainsItsExactRequirement()
    {
        VerifiedEntrySnapshot source = Observation(@"C:\source\file.txt", "source");
        VerifiedEntrySnapshot target = Observation(@"C:\target\file.txt", "target");
        FileContentHash hash = FileContentHash.Create(new string('0', 64));
        VerifiedCopyEntry file = VerifiedCopyEntry.File(source, target, hash, FileVerificationRequirements.OverwriteContents);
        VerifiedCopyEntry.VerifiedDirectory parent = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedDirectory>(
            VerifiedCopyEntry.Directory(Observation(@"C:\source", "parent-source"),
                Observation(@"C:\target", "parent-target"), [file]));

        VerifiedCopyEntry.VerifiedFile child = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedFile>(parent.Children[0]);
        Assert.AreSame(file, child);
        Assert.AreSame(FileVerificationRequirements.OverwriteContents, child.Requirements);
        Assert.AreSame(hash, child.Content);
        Assert.AreSame(source, child.Source);
        Assert.AreSame(target, child.Target);
        Assert.AreNotEqual(FileVerificationRequirements.DefaultStream, child.Requirements);
    }

    /// <summary>An absent requirement cannot silently choose the weaker default contract.</summary>
    [TestMethod]
    public void CreateWhenRequirementIsAbsentRejects()
    {
        VerifiedEntrySnapshot source = Observation(@"C:\source.txt", "source");
        VerifiedEntrySnapshot target = Observation(@"C:\target.txt", "target");

        ArgumentNullException failure = Assert.ThrowsExactly<ArgumentNullException>(() =>
            VerifiedCopyEntry.File(source, target, FileContentHash.Create(new string('0', 64)), null!));

        Assert.AreEqual("requirements", failure.ParamName);
    }

    private static VerifiedEntrySnapshot Observation(string path, string identity)
    {
        return VerifiedEntrySnapshot.Create(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path,
            Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse(identity)).Identity,
            FileObjectIdentity.Create(identity));
    }
}
