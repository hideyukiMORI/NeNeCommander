using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves initial native qualification is read-only, releases its leases and retains specific observed refusals.</summary>
[TestClass]
public sealed class WindowsOverwriteEligibilityTests
{
    /// <summary>Ordinary and empty files retain exact identities and known facts without creating preparations.</summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("desired")]
    public void InspectWhenPairIsAdmittedRetainsOriginalFactsWithoutEffects(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsLocalOperationTestContext.LocalSnapshot(root.WriteFile("source", content));
        FileEntrySnapshot target = WindowsLocalOperationTestContext.LocalSnapshot(root.WriteFile("target", "original"));

        TransferOverwriteCapability.QualifiedOverwrite result = Assert.IsInstanceOfType<TransferOverwriteCapability.QualifiedOverwrite>(
            WindowsOverwriteEligibility.Inspect(source, target.Path, new WindowsDriveBoundary()));

        Assert.AreEqual(source.Identity, result.Evidence.Source.Observation.Entry.Identity);
        Assert.AreEqual(target.Identity, result.Evidence.Target.Observation.Entry.Identity);
        Assert.AreEqual<long>(content.Length, result.Evidence.Source.Size.Bytes);
        Assert.AreEqual(8L, result.Evidence.Target.Size.Bytes);
        Assert.AreEqual(content, File.ReadAllText(source.Path.CanonicalText));
        Assert.AreEqual("original", File.ReadAllText(target.Path.CanonicalText));
        Assert.HasCount(2, Directory.GetFileSystemEntries(root.Path.CanonicalText));
        using FileStream sourceReleased = File.Open(source.Path.CanonicalText, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using FileStream targetReleased = File.Open(target.Path.CanonicalText, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    /// <summary>Both sides refuse known excluded shapes and preserve the source and target without preparations.</summary>
    [TestMethod]
    [DataRow("source", "folder", "folder")]
    [DataRow("target", "folder", "folder")]
    [DataRow("source", "hidden", "attributes")]
    [DataRow("target", "hidden", "attributes")]
    [DataRow("source", "hardlink", "links")]
    [DataRow("target", "hardlink", "links")]
    [DataRow("source", "emptyStream", "streams")]
    [DataRow("target", "emptyStream", "streams")]
    [DataRow("source", "stream", "streams")]
    [DataRow("target", "stream", "streams")]
    [DataRow("source", "junction", "attributes")]
    [DataRow("target", "junction", "attributes")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void InspectWhenObservedShapeIsExcludedReturnsSpecificRefusal(string side, string shape, string expected)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("source", "desired");
        _ = root.WriteFile("target", "original");
        ApplyShape(root, side, shape);
        FileEntrySnapshot source = WindowsLocalOperationTestContext.LocalSnapshot(root.Resolve("source"));
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.Resolve("target"));
        string[] before = Directory.GetFileSystemEntries(root.Path.CanonicalText);

        TransferOverwriteCapability.UnavailableOverwrite result = Assert.IsInstanceOfType<TransferOverwriteCapability.UnavailableOverwrite>(
            WindowsOverwriteEligibility.Inspect(source, target, new WindowsDriveBoundary()));

        Assert.AreSame(Reason(expected), result.Reason);
        CollectionAssert.AreEquivalent(before, Directory.GetFileSystemEntries(root.Path.CanonicalText));
        Assert.AreEqual(side == "source" ? "original" : "desired", File.ReadAllText(root.Resolve(side == "source" ? "target" : "source")));
        if (shape is not ("folder" or "junction"))
        {
            Assert.AreEqual(side == "source" ? "desired" : "original", File.ReadAllText(root.Resolve(side)));
        }
    }

    /// <summary>A missing target or changed frozen source yields unconfirmed capability without overwriting the ordinary path.</summary>
    [TestMethod]
    [DataRow("missingTarget")]
    [DataRow("changedSource")]
    [DataRow("exclusiveTarget")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void InspectWhenRequiredFactsCannotBeConfirmedRefusesWithoutEffects(string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsLocalOperationTestContext.LocalSnapshot(root.WriteFile("source", "desired"));
        string targetText = root.WriteFile("target", "original");
        if (defect == "missingTarget")
        {
            File.Delete(targetText);
        }
        if (defect == "changedSource")
        {
            _ = root.WriteFile("source", "changed-source");
        }
        using FileStream? occupied = defect == "exclusiveTarget" ? File.Open(targetText, FileMode.Open, FileAccess.ReadWrite, FileShare.None) : null;

        TransferOverwriteCapability.UnavailableOverwrite result = Assert.IsInstanceOfType<TransferOverwriteCapability.UnavailableOverwrite>(
            WindowsOverwriteEligibility.Inspect(source, WindowsLocalOperationTestContext.ParsePath(targetText), new WindowsDriveBoundary()));

        Assert.AreSame(TransferOverwriteUnavailableReason.Unconfirmed, result.Reason);
        Assert.AreEqual(defect == "changedSource" ? "changed-source" : "desired", File.ReadAllText(source.Path.CanonicalText));
        Assert.HasCount(defect == "missingTarget" ? 1 : 2, Directory.GetFileSystemEntries(root.Path.CanonicalText));
    }

    /// <summary>Path labels outside the admitted pair or the same lexical object never become overwrite authority.</summary>
    [TestMethod]
    [DataRow("same")]
    [DataRow("uncSource")]
    [DataRow("uncTarget")]
    public void InspectWhenPathsAreOutsideScopeRefusesThem(string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot local = WindowsLocalOperationTestContext.LocalSnapshot(root.WriteFile("source", "desired"));
        FileSystemPath remote = WindowsLocalOperationTestContext.ParsePath(@"\\server\share\file");
        FileEntrySnapshot source = defect == "uncSource" ? FileEntrySnapshot.Create(remote, local.Identity, local.DeletionCapability) : local;
        FileSystemPath target = defect == "uncTarget" ? remote : local.Path;

        TransferOverwriteCapability.UnavailableOverwrite result = Assert.IsInstanceOfType<TransferOverwriteCapability.UnavailableOverwrite>(
            WindowsOverwriteEligibility.Inspect(source, target, new WindowsDriveBoundary()));

        Assert.AreSame(defect == "same" ? TransferOverwriteUnavailableReason.SameObject : TransferOverwriteUnavailableReason.UnsupportedLocation, result.Reason);
        Assert.AreEqual("desired", File.ReadAllText(local.Path.CanonicalText));
    }

    /// <summary>Required provider inputs are not interpreted as an unsupported file.</summary>
    [TestMethod]
    public void InspectWhenInputsAreAbsentRejectsThem()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsLocalOperationTestContext.LocalSnapshot(root.WriteFile("source", "desired"));
        WindowsDriveBoundary boundary = new();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsOverwriteEligibility.Inspect(null!, source.Path, boundary));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsOverwriteEligibility.Inspect(source, null!, boundary));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsOverwriteEligibility.Inspect(source, source.Path, null!));
    }

    private static TransferOverwriteUnavailableReason Reason(string expected)
    {
        return expected switch
        {
            "folder" => TransferOverwriteUnavailableReason.Folder,
            "attributes" => TransferOverwriteUnavailableReason.SpecialAttributes,
            "links" => TransferOverwriteUnavailableReason.MultipleLinks,
            "streams" => TransferOverwriteUnavailableReason.StreamsUnconfirmed,
            _ => throw new ArgumentOutOfRangeException(nameof(expected)),
        };
    }

    private static void ApplyShape(TestOwnedTemporaryRoot root, string side, string shape)
    {
        switch (shape)
        {
            case "folder":
                File.Delete(root.Resolve(side));
                _ = root.CreateDirectory(side);
                break;
            case "junction":
                File.Delete(root.Resolve(side));
                _ = root.CreateDirectory("foreign");
                _ = root.CreateJunction(side, "foreign");
                break;
            case "hidden":
                File.SetAttributes(root.Resolve(side), FileAttributes.Hidden);
                break;
            case "hardlink":
                _ = root.CreateHardLink("alias", side);
                break;
            case "emptyStream":
            case "stream":
                _ = root.WriteFile(side + ":extra", shape == "emptyStream" ? string.Empty : "extra");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }
    }
}
