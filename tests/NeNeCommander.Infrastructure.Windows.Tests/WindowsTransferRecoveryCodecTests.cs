using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves exact saved observations, append binding and every possible truncation without filesystem effects.</summary>
[TestClass]
public sealed class WindowsTransferRecoveryCodecTests
{
    /// <summary>Proves exact casing, normalization form and supplementary Unicode survive canonical encoding.</summary>
    [TestMethod]
    [DataRow("source.txt")]
    [DataRow("日本語-é-e\u0301-\U0001F4C1.txt")]
    public void InspectWhenInitialAndCompletionAreValidPreservesExactMapping(string name)
    {
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(@"D:\recovery\" + name);
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        byte[] append = WindowsTransferRecoveryCodec.EncodeCompletion(initial);

        WindowsTransferRecoveryPrepared prepared = Assert.IsInstanceOfType<WindowsTransferRecoveryPrepared>(WindowsTransferRecoveryCodec.Inspect(initial));
        WindowsTransferRecoveryCompleted completed = Assert.IsInstanceOfType<WindowsTransferRecoveryCompleted>(WindowsTransferRecoveryCodec.Inspect(WindowsTransferRecoveryTestFixture.Join(initial, append)));
        Assert.AreEqual(mapping, prepared.Mapping);
        Assert.AreEqual(mapping, completed.Mapping);
        Assert.AreEqual(0L, prepared.IncompleteTail.Bytes);
        Assert.AreEqual(prepared.Initial, completed.Initial);
        Assert.AreEqual(append.Length, completed.Completion.Size.Bytes);
        CollectionAssert.AreEqual(initial, WindowsTransferRecoveryCodec.EncodeInitial(mapping));
    }

    /// <summary>Proves no truncated initial mapping can be reported as prepared or complete.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void InspectWhenInitialIsTruncatedAtAnyByteRefusesIt()
    {
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(WindowsTransferRecoveryTestFixture.Mapping());
        for (int length = 0; length < initial.Length; length++)
        {
            int captured = length;
            _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryCodec.Inspect(initial.AsMemory(0, captured)));
        }
    }

    /// <summary>Proves every incomplete append preserves initial evidence and never asserts desired-content completion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void InspectWhenAppendIsTruncatedAtAnyByteRetainsPreparedObservation()
    {
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping();
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        byte[] append = WindowsTransferRecoveryCodec.EncodeCompletion(initial);
        for (int length = 0; length < append.Length; length++)
        {
            byte[] partial = WindowsTransferRecoveryTestFixture.Join(initial, append.AsSpan(0, length));
            WindowsTransferRecoveryPrepared observed = Assert.IsInstanceOfType<WindowsTransferRecoveryPrepared>(WindowsTransferRecoveryCodec.Inspect(partial));
            Assert.AreEqual(mapping, observed.Mapping);
            Assert.AreEqual(length, observed.IncompleteTail.Bytes);
        }
    }

    /// <summary>Proves a repeated token and identical target/content fields cannot authorize an append from another initial mapping.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void InspectWhenAppendBelongsToDifferentInitialWithSameTokenRefusesIt()
    {
        WindowsTransferRecoveryMapping first = WindowsTransferRecoveryTestFixture.Mapping();
        WindowsTransferRecoveryMapping second = WindowsTransferRecoveryTestFixture.Mapping(@"D:\recovery\different-source.txt");
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(first);
        byte[] foreignAppend = WindowsTransferRecoveryCodec.EncodeCompletion(WindowsTransferRecoveryCodec.EncodeInitial(second));
        Assert.AreEqual(first.Names.Token, second.Names.Token);
        Assert.AreEqual(first.Files.Target.ObjectIdentity, second.Files.Target.ObjectIdentity);
        Assert.AreEqual(first.Desired, second.Desired);

        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryCodec.Inspect(WindowsTransferRecoveryTestFixture.Join(initial, foreignAppend)));
    }

    /// <summary>Proves a third frame, its prefix, or any byte after completion is rejected.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(74)]
    [DataRow(0)]
    public void InspectWhenBytesFollowCompleteAppendRefusesThem(int extraLength)
    {
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(WindowsTransferRecoveryTestFixture.Mapping());
        byte[] append = WindowsTransferRecoveryCodec.EncodeCompletion(initial);
        byte[] complete = WindowsTransferRecoveryTestFixture.Join(initial, append);
        byte[] extra = extraLength == 0 ? append : append.AsSpan(0, extraLength).ToArray();

        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryCodec.Inspect(WindowsTransferRecoveryTestFixture.Join(complete, extra)));
    }

    /// <summary>Proves the completion writer binds only one exact, complete initial frame.</summary>
    [TestMethod]
    [DataRow("partial")]
    [DataRow("trailing")]
    [DataRow("complete")]
    public void EncodeCompletionWhenInputIsNotExactlyInitialRefusesIt(string defect)
    {
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(WindowsTransferRecoveryTestFixture.Mapping());
        byte[] input = defect switch
        {
            "partial" => initial.AsSpan(0, initial.Length - 1).ToArray(),
            "trailing" => WindowsTransferRecoveryTestFixture.Join(initial, "0"u8),
            "complete" => WindowsTransferRecoveryTestFixture.Join(initial, WindowsTransferRecoveryCodec.EncodeCompletion(initial)),
            _ => throw new ArgumentOutOfRangeException(nameof(defect)),
        };

        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryCodec.EncodeCompletion(input));
    }

    /// <summary>Proves the documented path/identity worst-case escapes and length extremes remain inside the frame bound.</summary>
    [TestMethod]
    public void InspectWhenTextAndLengthsReachBoundsRoundTripsWithoutShortening()
    {
        WindowsLocalPath source = WindowsTransferRecoveryTestFixture.Path(@"D:\" + new string('日', 32764));
        WindowsLocalPath target = WindowsTransferRecoveryTestFixture.Path(@"D:\" + new string('日', 32600) + @"\target");
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(target, Guid.Empty);
        WindowsTransferRecoveryFiles files = WindowsTransferRecoveryFiles.Create(MaximumEntry(source, 'A'), MaximumEntry(target, 'B'),
            MaximumEntry(names.OriginalBackup, 'C'), MaximumEntry(names.PreparedContent, 'D'));
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryMapping.Create(Guid.Empty, files,
            WindowsTransferRecoveryTestFixture.Content(0, 'A'), WindowsTransferRecoveryTestFixture.Content(long.MaxValue, 'F'));
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(mapping);

        Assert.IsLessThanOrEqualTo(WindowsTransferRecoveryFrame.MaximumFrameLength, initial.Length);
        Assert.AreEqual(32767, mapping.Files.Source.Path.CanonicalText.Length);
        Assert.AreEqual(mapping, WindowsTransferRecoveryCodec.Inspect(initial).Mapping);
        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryCodec.Inspect(new byte[WindowsTransferRecoveryFrame.MaximumRecordLength + 1]));
    }

    /// <summary>Proves unpaired input UTF-16 is refused rather than silently replaced during serialization.</summary>
    [TestMethod]
    [DataRow(0xD800)]
    [DataRow(0xDC00)]
    public void EncodeInitialWhenPathContainsUnpairedSurrogateRefusesIt(int codeUnit)
    {
        string invalid = new((char)codeUnit, 1);
        Assert.IsTrue(char.IsSurrogate(invalid[0]));
        Assert.AreEqual(codeUnit, invalid[0]);
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(@"D:\recovery\" + invalid);

        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryCodec.EncodeInitial(mapping));
    }

    private static VerifiedEntrySnapshot MaximumEntry(WindowsLocalPath path, char suffix)
    {
        string value = new string('日', 511) + suffix;
        FileIdentity full = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse(value)).Identity;
        return VerifiedEntrySnapshot.Create(path, full, FileObjectIdentity.Create(value));
    }
}
