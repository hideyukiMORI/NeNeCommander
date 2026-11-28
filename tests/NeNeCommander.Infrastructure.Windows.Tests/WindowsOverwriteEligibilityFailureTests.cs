using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves adapter admission failures remain unavailable capability without effects or exception escape.</summary>
[TestClass]
public sealed class WindowsOverwriteEligibilityFailureTests
{
    /// <summary>Expected boundary failures cannot qualify a pair or escape the capability probe.</summary>
    [TestMethod]
    [DataRow("access")]
    [DataRow("argument")]
    [DataRow("unsupported")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void InspectWhenDriveAdapterCannotObserveReturnsUnconfirmedWithoutEffects(string failure)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsLocalOperationTestContext.LocalSnapshot(root.WriteFile("source", "source"));
        FileSystemPath target = WindowsLocalOperationTestContext.ParsePath(root.WriteFile("target", "target"));
        int calls = 0;
        WindowsDriveBoundary boundary = new(_ =>
        {
            calls++;
            throw Failure(failure);
        });

        TransferOverwriteCapability.UnavailableOverwrite result = Assert.IsInstanceOfType<TransferOverwriteCapability.UnavailableOverwrite>(
            WindowsOverwriteEligibility.Inspect(source, target, boundary));

        Assert.AreSame(TransferOverwriteUnavailableReason.Unconfirmed, result.Reason);
        Assert.AreEqual(1, calls);
        Assert.HasCount(2, Directory.GetFileSystemEntries(root.Path.CanonicalText));
        Assert.AreEqual("source", File.ReadAllText(source.Path.CanonicalText));
        Assert.AreEqual("target", File.ReadAllText(target.CanonicalText));
        using FileStream sourceReleased = File.Open(source.Path.CanonicalText, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using FileStream targetReleased = File.Open(target.CanonicalText, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    private static Exception Failure(string failure)
    {
        return failure switch
        {
            "access" => new UnauthorizedAccessException("Injected unavailable drive access."),
            "argument" => new ArgumentException("Injected unobservable drive path."),
            "unsupported" => new NotSupportedException("Injected unsupported drive observation."),
            _ => throw new ArgumentOutOfRangeException(nameof(failure)),
        };
    }
}
