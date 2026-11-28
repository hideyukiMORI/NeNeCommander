using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves exact pair admission retains source and target and closes every failed opening.</summary>
[TestClass]
public sealed class WindowsOverwritePairLeaseTests
{
    /// <summary>Proves ordinary and empty file observations retain the canonical identity without changing bytes.</summary>
    [TestMethod]
    [DataRow("source", "target")]
    [DataRow("", "target")]
    [DataRow("source", "")]
    [DataRow("", "")]
    public void OpenWhenPairIsEligibleRetainsExactFactsAndBytes(string sourceContent, string targetContent)
    {
        ArgumentNullException.ThrowIfNull(sourceContent);
        ArgumentNullException.ThrowIfNull(targetContent);
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = Snapshot(root.WriteFile("source.txt", sourceContent));
        WindowsLocalPath target = Parse(root.WriteFile("target.txt", targetContent));
        FileIdentity targetIdentity = WindowsLocalEntryIdentity.Describe(new FileInfo(target.CanonicalText));
        TransferOverwriteEvidence evidence;
        using (WindowsOverwritePairLease pair = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary()))
        {
            evidence = pair.Evidence;
            pair.Revalidate(evidence);
            Assert.AreEqual(source.Identity, evidence.Source.Observation.Entry.Identity);
            Assert.AreEqual(targetIdentity, evidence.Target.Observation.Entry.Identity);
            Assert.AreEqual(sourceContent.Length, evidence.Source.Size.Bytes);
            Assert.AreEqual(targetContent.Length, evidence.Target.Size.Bytes);
            Assert.AreEqual(source.Path, pair.Source.Path);
            Assert.AreEqual(target, pair.Target.Path);
        }
        Assert.AreEqual(sourceContent, File.ReadAllText(source.Path.CanonicalText));
        Assert.AreEqual(targetContent, File.ReadAllText(target.CanonicalText));
        using WindowsOverwritePairLease reopened = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary());
        reopened.Revalidate(evidence);
    }

    /// <summary>Proves a target opening failure releases the already admitted source chain.</summary>
    [TestMethod]
    [DataRow("missing")]
    [DataRow("directory")]
    [DataRow("writer")]
    public void OpenWhenTargetCannotBeAdmittedReleasesSource(string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = Snapshot(root.WriteFile("source.txt", "source"));
        string target = defect == "directory" ? root.CreateDirectory("target") : root.Resolve("target");
        using SafeFileHandle? writer = defect == "writer"
            ? File.OpenHandle(target, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete) : null;

        _ = Assert.Throws<IOException>(() =>
        {
            using WindowsOverwritePairLease refused = WindowsOverwritePairLease.Open(source, Parse(target), new WindowsDriveBoundary());
        });
        Assert.AreEqual("source", File.ReadAllText(root.Resolve("source.txt")));
        using SafeFileHandle sourceWriter = File.OpenHandle(source.Path.CanonicalText, FileMode.Open, FileAccess.Write, FileShare.None);
    }

    /// <summary>Proves named streams, aliases and attributes on either side refuse without losing the files.</summary>
    [TestMethod]
    [DataRow("source", "emptyStream")]
    [DataRow("target", "emptyStream")]
    [DataRow("source", "stream")]
    [DataRow("target", "stream")]
    [DataRow("source", "hardlink")]
    [DataRow("target", "hardlink")]
    [DataRow("source", "hidden")]
    [DataRow("target", "hidden")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void OpenWhenEitherFileIsIneligibleRefusesAndReleasesBoth(string side, string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("source.txt", "source");
        string targetText = root.WriteFile("target.txt", "target");
        ApplyDefect(root, side, defect);
        FileEntrySnapshot source = Snapshot(sourceText);

        _ = Assert.Throws<IOException>(() =>
        {
            using WindowsOverwritePairLease refused = WindowsOverwritePairLease.Open(source, Parse(targetText), new WindowsDriveBoundary());
        });
        Assert.AreEqual("source", File.ReadAllText(sourceText));
        Assert.AreEqual("target", File.ReadAllText(targetText));
        using SafeFileHandle sourceWriter = File.OpenHandle(sourceText, FileMode.Open, FileAccess.Write, FileShare.None);
        using SafeFileHandle targetWriter = File.OpenHandle(targetText, FileMode.Open, FileAccess.Write, FileShare.None);
    }

    /// <summary>Proves late observed aliases or attributes cannot retain a formerly eligible capability.</summary>
    [TestMethod]
    [DataRow("source", "emptyStream")]
    [DataRow("target", "emptyStream")]
    [DataRow("source", "stream")]
    [DataRow("target", "stream")]
    [DataRow("source", "hardlink")]
    [DataRow("target", "hardlink")]
    [DataRow("source", "hidden")]
    [DataRow("target", "hidden")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void RevalidateWhenEitherFileBecomesIneligibleRefusesOriginalEvidence(string side, string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = Snapshot(root.WriteFile("source.txt", "source"));
        WindowsLocalPath target = Parse(root.WriteFile("target.txt", "target"));
        using WindowsOverwritePairLease pair = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary());
        TransferOverwriteEvidence original = pair.Evidence;
        ApplyDefect(root, side, defect);

        _ = Assert.Throws<IOException>(() => pair.Revalidate(original));
        Assert.AreSame(original, pair.Evidence);
    }

    /// <summary>Proves a rewrite under the owned target handle cannot be accepted by refreshing the baseline.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void RevalidateWhenTargetLengthChangesRefusesWithoutReplacingEvidence()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = Snapshot(root.WriteFile("source.txt", "source"));
        WindowsLocalPath target = Parse(root.WriteFile("target.txt", "target"));
        using WindowsOverwritePairLease pair = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary());
        TransferOverwriteEvidence original = pair.Evidence;
        RandomAccess.SetLength(pair.Target.Handle, 0);

        _ = Assert.Throws<IOException>(() => pair.Revalidate(original));
        Assert.AreSame(original, pair.Evidence);
        Assert.AreEqual(6L, original.Target.Size.Bytes);
    }

    /// <summary>Proves prior target consent and frozen source identity cannot be silently refreshed.</summary>
    [TestMethod]
    public void OpenAndRevalidateWhenOriginalFactsChangedRefuseThem()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = Snapshot(root.WriteFile("source.txt", "source"));
        WindowsLocalPath target = Parse(root.WriteFile("target.txt", "target"));
        TransferOverwriteEvidence original;
        using (WindowsOverwritePairLease first = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary()))
        {
            original = first.Evidence;
        }
        _ = root.WriteFile("target.txt", "new target");
        using (WindowsOverwritePairLease second = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary()))
        {
            _ = Assert.Throws<IOException>(() => second.Revalidate(original));
        }
        _ = root.WriteFile("source.txt", "new source");
        _ = Assert.Throws<IOException>(() =>
        {
            using WindowsOverwritePairLease refused = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary());
        });
        using SafeFileHandle sourceWriter = File.OpenHandle(source.Path.CanonicalText, FileMode.Open, FileAccess.Write, FileShare.None);
        using SafeFileHandle targetWriter = File.OpenHandle(target.CanonicalText, FileMode.Open, FileAccess.Write, FileShare.None);
    }

    /// <summary>Proves lexical and native-object aliases cannot enter a read/write pair.</summary>
    [TestMethod]
    [DataRow("same")]
    [DataRow("hardlink")]
    public void OpenWhenSourceAndTargetAliasRefusesThem(string alias)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = Snapshot(root.WriteFile("source.txt", "source"));
        string target = alias == "same" ? source.Path.CanonicalText : root.CreateHardLink("target.txt", "source.txt");

        _ = Assert.Throws<IOException>(() =>
        {
            using WindowsOverwritePairLease refused = WindowsOverwritePairLease.Open(source, Parse(target), new WindowsDriveBoundary());
        });
        Assert.AreEqual("source", File.ReadAllText(source.Path.CanonicalText));
    }

    /// <summary>Proves unsupported source variants and root paths refuse before opening either file.</summary>
    [TestMethod]
    [DataRow("sourceRoot")]
    [DataRow("targetRoot")]
    [DataRow("remoteSource")]
    public void OpenWhenPathsAreUnsupportedRefusesWithoutEffects(string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot ordinary = Snapshot(root.WriteFile("source.txt", "source"));
        FileSystemPath path = defect switch
        {
            "sourceRoot" => Parse(@"C:\"),
            "remoteSource" => Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(@"\\server\share\source.txt")).Path,
            _ => ordinary.Path,
        };
        FileEntrySnapshot source = FileEntrySnapshot.Create(path, ordinary.Identity, ordinary.DeletionCapability);
        WindowsLocalPath target = Parse(defect == "targetRoot" ? @"D:\" : root.WriteFile("target.txt", "target"));

        _ = Assert.Throws<IOException>(() =>
        {
            using WindowsOverwritePairLease refused = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary());
        });
        Assert.AreEqual("source", File.ReadAllText(ordinary.Path.CanonicalText));
    }

    /// <summary>Proves absent inputs and a disposed pair cannot supply usable evidence.</summary>
    [TestMethod]
    public void OpenAndRevalidateWhenRequiredOwnershipIsAbsentRefuseIt()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = Snapshot(root.WriteFile("source.txt", "source"));
        WindowsLocalPath target = Parse(root.WriteFile("target.txt", "target"));
        WindowsDriveBoundary boundary = new();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsOverwritePairLease.Open(null!, target, boundary));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsOverwritePairLease.Open(source, null!, boundary));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsOverwritePairLease.Open(source, target, null!));
        using WindowsOverwritePairLease pair = WindowsOverwritePairLease.Open(source, target, boundary);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => pair.Revalidate(null!));
        pair.Dispose();
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => pair.Revalidate(pair.Evidence));
    }

    private static void ApplyDefect(TestOwnedTemporaryRoot root, string side, string defect)
    {
        string child = side + ".txt";
        switch (defect)
        {
            case "emptyStream":
            case "stream":
                _ = root.WriteFile(child + ":Zone.Identifier", defect == "stream" ? "zone" : string.Empty);
                break;
            case "hardlink":
                _ = root.CreateHardLink("alias.txt", child);
                break;
            case "hidden":
                File.SetAttributes(root.Resolve(child), FileAttributes.Hidden);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(defect));
        }
    }

    private static FileEntrySnapshot Snapshot(string path)
    {
        return FileEntrySnapshot.Create(Parse(path), WindowsLocalEntryIdentity.Describe(new FileInfo(path)), DeletionCapability.NotRequested);
    }

    private static WindowsLocalPath Parse(string path)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path);
    }
}
