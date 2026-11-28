using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Checks admission precedence and distinct diagnostics without allowing a later boundary failure to stand in for the intended refusal.</summary>
[TestClass]
public sealed class WindowsOverwriteLeaseRefusalTests
{
    /// <summary>Missing drive ownership refuses before lexical alias validation, with no new native access.</summary>
    [TestMethod]
    public void OpenWhenDriveBoundaryIsAbsentRejectsBeforeAnInvalidPair()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        FileEntrySnapshot source = FileEntrySnapshot.Create(fixture.Pair.Source.Path, fixture.Pair.Source.Baseline.Identity, DeletionCapability.NotRequested);

        TransferArgumentAssertions.Rejects(() =>
        {
            using WindowsOverwritePairLease refused = WindowsOverwritePairLease.Open(source, fixture.Pair.Source.Path, null!);
        }, "driveBoundary");
        IOException alias = Assert.ThrowsExactly<IOException>(() =>
        {
            using WindowsOverwritePairLease refused = WindowsOverwritePairLease.Open(source, fixture.Pair.Source.Path, new WindowsDriveBoundary());
        });

        Assert.Contains("distinct non-root", alias.Message);
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }

    /// <summary>A frozen source from another object is reported as a source-identity failure and releases both newly opened chains.</summary>
    [TestMethod]
    public void OpenWhenFrozenSourceIdentityIsForeignExplainsRefusalAndReleasesBothFiles()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        FileEntrySnapshot source = FileEntrySnapshot.Create(fixture.Pair.Source.Path, fixture.Pair.Target.Baseline.Identity, DeletionCapability.NotRequested);
        fixture.Pair.Dispose();

        IOException failure = Assert.ThrowsExactly<IOException>(() =>
        {
            using WindowsOverwritePairLease refused = WindowsOverwritePairLease.Open(source, fixture.Pair.Target.Path, new WindowsDriveBoundary());
        });

        Assert.Contains("source identity", failure.Message);
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
        using SafeFileHandle sourceWriter = File.OpenHandle(source.Path.CanonicalText, FileMode.Open, FileAccess.Write, FileShare.None);
        using SafeFileHandle targetWriter = File.OpenHandle(fixture.Pair.Target.Path.CanonicalText, FileMode.Open, FileAccess.Write, FileShare.None);
    }

    /// <summary>Foreign confirmation never silently replaces the pair's exact evidence, and the diagnostic identifies consent mismatch.</summary>
    [TestMethod]
    public void RevalidateWhenConfirmationBelongsToAnotherPairPreservesBothOriginalOwners()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsOverwritePreparationFixture foreign = WindowsOverwritePreparationFixture.Create("other source", "other target");
        TransferOverwriteEvidence original = fixture.Pair.Evidence;

        IOException failure = Assert.ThrowsExactly<IOException>(() => fixture.Pair.Revalidate(foreign.Pair.Evidence));

        Assert.Contains("confirmation", failure.Message);
        Assert.AreSame(original, fixture.Pair.Evidence);
        fixture.Pair.Revalidate(original);
        foreign.Pair.Revalidate(foreign.Pair.Evidence);
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
        Assert.AreEqual("other target", foreign.Read(foreign.Pair.Target.Path));
    }
}
