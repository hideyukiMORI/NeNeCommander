using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Separates exact local-volume prefix admission from GUID and separator shape.</summary>
[TestClass]
public sealed class WindowsTransferVolumeNameTests
{
    /// <summary>A valid GUID tail and separators cannot qualify an altered or differently cased native prefix.</summary>
    [TestMethod]
    [DataRow(@"xx?xVolume{12345678-1234-1234-1234-123456789abc}\")]
    [DataRow(@"\\?\volume{12345678-1234-1234-1234-123456789abc}\")]
    [DataRow(@"\\?\Volume[12345678-1234-1234-1234-123456789abc}\")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void HasVolumeGuidWhenOnlyPrefixDiffersRefusesAmbiguousNativeText(string path)
    {
        Assert.IsNotNull(path);
        Assert.AreEqual(49, path.Length);
        Assert.IsTrue(WindowsTransferPathObservation.HasVolumeGuid(@"\\?\Volume{12345678-1234-1234-1234-123456789abc}\"));
        Assert.IsFalse(WindowsTransferPathObservation.HasVolumeGuid(path));
    }
}
