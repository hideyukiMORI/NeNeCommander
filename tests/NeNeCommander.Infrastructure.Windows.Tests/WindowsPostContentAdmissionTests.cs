using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves post-effect observation keeps the original native ownership chain without refreshing original confirmation.</summary>
[TestClass]
public sealed class WindowsPostContentAdmissionTests
{
    /// <summary>Proves actual write or empty truncation changes full identity but never changes or refreshes the admitted native object baseline.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(3)]
    public void ObserveAfterContentChangeWhenHeldObjectWasWrittenReportsNewFactsButOldConsentStillRefuses(int length)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "original-target");
        TransferOverwriteEvidence original = fixture.Pair.Evidence;
        RandomAccess.Write(fixture.Pair.Target.Handle, new byte[length], 0);
        RandomAccess.SetLength(fixture.Pair.Target.Handle, length);

        fixture.Pair.Target.RevalidateObject();
        TransferOverwriteFileObservation current = WindowsOverwriteFileAdmission.ObserveAfterContentChange(fixture.Pair.Target);
        Assert.AreEqual(length, current.Size.Bytes);
        Assert.AreEqual(original.Target.Observation.Entry.ObjectIdentity, current.Observation.Entry.ObjectIdentity);
        Assert.AreNotEqual(original.Target.Observation.Entry.Identity, current.Observation.Entry.Identity);
        Assert.AreSame(original, fixture.Pair.Evidence);
        Assert.AreEqual(original.Target.Observation.Entry, fixture.Pair.Target.Baseline);
        _ = Assert.Throws<IOException>(fixture.Pair.Target.Revalidate);
        _ = Assert.Throws<IOException>(() => WindowsOverwriteFileAdmission.Observe(fixture.Pair.Target));
        _ = Assert.Throws<IOException>(() => fixture.Pair.Revalidate(original));
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
    }

    /// <summary>Proves post-effect admission still observes empty streams, aliases and attributes introduced during its final native path check.</summary>
    [TestMethod]
    [DataRow("stream")]
    [DataRow("alias")]
    [DataRow("hidden")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void ObserveAfterContentChangeWhenFinalPathObservationChangesShapeRefusesIt(string defect)
    {
        WindowsOverwritePreparationFixture? observed = null;
        bool armed = false;
        int visits = 0;
        WindowsDriveBoundary boundary = new(_ =>
        {
            if (armed && ++visits == 2)
            {
                ChangeTarget(observed ?? throw new AssertFailedException(), defect);
            }
            return DriveType.Fixed;
        });
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "original-target", driveBoundary: boundary);
        observed = fixture;
        RandomAccess.SetLength(fixture.Pair.Target.Handle, 0);
        armed = true;

        _ = Assert.Throws<IOException>(() => WindowsOverwriteFileAdmission.ObserveAfterContentChange(fixture.Pair.Target));
        Assert.AreEqual(2, visits);
        Assert.AreEqual(0L, RandomAccess.GetLength(fixture.Pair.Target.Handle));
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
    }

    /// <summary>Proves post-effect observation retains drive admission and lifetime refusal instead of skipping the owning chain.</summary>
    [TestMethod]
    [DataRow("network")]
    [DataRow("unknown")]
    [DataRow("disposed")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public void RevalidateObjectWhenOwnedBoundaryIsLostRefusesIt(string defect)
    {
        DriveType current = DriveType.Fixed;
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target", driveBoundary: new WindowsDriveBoundary(_ => current));
        RandomAccess.SetLength(fixture.Pair.Target.Handle, 0);
        if (defect == "disposed")
        {
            fixture.Pair.Target.Dispose();
            _ = Assert.ThrowsExactly<ObjectDisposedException>(fixture.Pair.Target.RevalidateObject);
        }
        else
        {
            current = defect == "network" ? DriveType.Network : DriveType.Unknown;
            _ = Assert.ThrowsExactly<WindowsDriveBoundaryRejectedException>(fixture.Pair.Target.RevalidateObject);
            _ = Assert.ThrowsExactly<WindowsDriveBoundaryRejectedException>(() => WindowsOverwriteFileAdmission.ObserveAfterContentChange(fixture.Pair.Target));
        }
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
    }

    private static void ChangeTarget(WindowsOverwritePreparationFixture fixture, string defect)
    {
        switch (defect)
        {
            case "stream":
                _ = fixture.Root.WriteFile("target:Zone.Identifier", string.Empty);
                break;
            case "alias":
                _ = fixture.Root.CreateHardLink("target-alias", "target");
                break;
            case "hidden":
                File.SetAttributes(fixture.Pair.Target.Path.CanonicalText, FileAttributes.Hidden);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(defect));
        }
    }
}
