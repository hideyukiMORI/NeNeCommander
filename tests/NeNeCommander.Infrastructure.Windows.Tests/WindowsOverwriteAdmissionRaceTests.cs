using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Injects changes at existing drive-observation boundaries so later disqualifying facts cannot be ignored.</summary>
[TestClass]
public sealed class WindowsOverwriteAdmissionRaceTests
{
    /// <summary>Proves the final path revalidation cannot discard a newly observed attribute or link change.</summary>
    [TestMethod]
    [DataRow("hidden")]
    [DataRow("hardlink")]
    [DataRow("emptyStream")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void ObserveWhenFinalPathCheckReadsDisqualifyingFactsRefusesThem(string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = Parse(root.WriteFile("source.txt", "source"));
        bool armed = false;
        int revalidations = 0;
        WindowsDriveBoundary boundary = new(_ =>
        {
            if (armed && ++revalidations == 2)
            {
                ChangeSource(root, defect);
            }
            return DriveType.Fixed;
        });
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(path, FileAccess.Read, boundary);
        armed = true;

        _ = Assert.Throws<IOException>(() => WindowsOverwriteFileAdmission.Observe(lease));
        Assert.AreEqual(2, revalidations);
        Assert.AreEqual("source", File.ReadAllText(path.CanonicalText));
    }

    /// <summary>Proves a source change during target observation is not discarded by the final source identity-only read.</summary>
    [TestMethod]
    [DataRow("hidden")]
    [DataRow("hardlink")]
    [DataRow("emptyStream")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void RevalidateWhenTargetObservationChangesSourceRefusesItsLaterFacts(string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath sourcePath = Parse(root.WriteFile("source.txt", "source"));
        WindowsLocalPath target = Parse(root.WriteFile("target.txt", "target"));
        FileEntrySnapshot source = FileEntrySnapshot.Create(sourcePath,
            WindowsLocalEntryIdentity.Describe(new FileInfo(sourcePath.CanonicalText)), DeletionCapability.NotRequested);
        bool armed = false;
        WindowsDriveBoundary boundary = new(path =>
        {
            if (armed && path == target)
            {
                armed = false;
                ChangeSource(root, defect);
            }
            return DriveType.Fixed;
        });
        using WindowsOverwritePairLease pair = WindowsOverwritePairLease.Open(source, target, boundary);
        TransferOverwriteEvidence original = pair.Evidence;
        armed = true;

        _ = Assert.Throws<IOException>(() => pair.Revalidate(original));
        Assert.IsFalse(armed);
        Assert.AreSame(original, pair.Evidence);
    }

    private static void ChangeSource(TestOwnedTemporaryRoot root, string defect)
    {
        switch (defect)
        {
            case "hidden":
                File.SetAttributes(root.Resolve("source.txt"), FileAttributes.Hidden);
                break;
            case "emptyStream":
                _ = root.WriteFile("source.txt:Zone.Identifier", string.Empty);
                break;
            case "hardlink":
                _ = root.CreateHardLink("alias.txt", "source.txt");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(defect));
        }
    }

    private static WindowsLocalPath Parse(string path)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path);
    }
}
