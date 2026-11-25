using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves checksum-valid but semantically malformed recovery records cannot become accepted observations.</summary>
[TestClass]
public sealed class WindowsTransferRecoveryCorruptionTests
{
    /// <summary>Proves strict schema, exact text and file relationships after a valid outer checksum.</summary>
    [TestMethod]
    [DataRow("version")]
    [DataRow("versionType")]
    [DataRow("phase")]
    [DataRow("duplicateRoot")]
    [DataRow("duplicateNested")]
    [DataRow("unknownRoot")]
    [DataRow("unknownNested")]
    [DataRow("unknownContent")]
    [DataRow("missing")]
    [DataRow("null")]
    [DataRow("negative")]
    [DataRow("overflow")]
    [DataRow("lengthType")]
    [DataRow("lengthFraction")]
    [DataRow("identityLength")]
    [DataRow("badHash")]
    [DataRow("hashLength")]
    [DataRow("canonicalPath")]
    [DataRow("pathAlias")]
    [DataRow("objectAlias")]
    [DataRow("candidateCase")]
    [DataRow("candidateName")]
    [DataRow("unicode")]
    [DataRow("depth")]
    [DataRow("trailingJson")]
    [DataRow("comments")]
    [DataRow("whitespace")]
    [DataRow("incompleteJson")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void InspectWhenChecksumValidInitialHasInvalidSchemaRefusesIt(string defect)
    {
        string original = Encoding.UTF8.GetString(WindowsTransferRecoveryJson.WriteInitial(WindowsTransferRecoveryTestFixture.Mapping()));
        string invalid = Corrupt(original, defect);
        Assert.AreNotEqual(original, invalid);
        byte[] frame = WindowsTransferRecoveryTestFixture.JsonFrame(invalid);

        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryCodec.Inspect(frame));
    }

    /// <summary>Proves a known malformed suffix cannot be treated as an incomplete valid append.</summary>
    [TestMethod]
    [DataRow("X")]
    [DataRow("00000000")]
    [DataRow("00100001")]
    [DataRow("FFFFFFFF")]
    [DataRow("00000003;")]
    public void InspectWhenTrailingHeaderIsMalformedRefusesIt(string suffix)
    {
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(WindowsTransferRecoveryTestFixture.Mapping());
        byte[] record = WindowsTransferRecoveryTestFixture.Join(initial, Encoding.ASCII.GetBytes(suffix));

        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryCodec.Inspect(record));
    }

    /// <summary>Proves a complete append cannot hide checksum or exact expected-field defects.</summary>
    [TestMethod]
    [DataRow("checksum")]
    [DataRow("target")]
    [DataRow("desired")]
    [DataRow("duplicate")]
    [DataRow("unknown")]
    [DataRow("phase")]
    [DataRow("unicode")]
    public void InspectWhenCompleteAppendIsCorruptRefusesIt(string defect)
    {
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(WindowsTransferRecoveryTestFixture.Mapping());
        byte[] append = WindowsTransferRecoveryCodec.EncodeCompletion(initial);
        string json = WindowsTransferRecoveryFrame.Parse(append).Json;
        if (defect == "checksum")
        {
            append[9] = append[9] == (byte)'0' ? (byte)'1' : (byte)'0';
        }
        else
        {
            string invalid = defect switch
            {
                "target" => json.Replace("object-target", "object-other", StringComparison.Ordinal),
                "desired" => json.Replace(new string('B', 64), new string('C', 64), StringComparison.Ordinal),
                "duplicate" => json.Replace("\"schemaVersion\": 1,", "\"schemaVersion\": 1,\n\"schemaVersion\": 1,", StringComparison.Ordinal),
                "unknown" => json.Insert(1, "\n\"unknown\": 0,"),
                "phase" => json.Replace("DesiredContentVerified", "PreparedWithTargetPossiblyChanging", StringComparison.Ordinal),
                "unicode" => json.Replace("object-target", "\\uD800", StringComparison.Ordinal),
                _ => throw new ArgumentOutOfRangeException(nameof(defect)),
            };
            Assert.AreNotEqual(json, invalid);
            append = WindowsTransferRecoveryTestFixture.JsonFrame(invalid);
        }

        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryCodec.Inspect(WindowsTransferRecoveryTestFixture.Join(initial, append)));
    }

    /// <summary>Proves an incomplete length field already exceeding every valid completion is malformed, not an ignorable fragment.</summary>
    [TestMethod]
    [DataRow("F")]
    [DataRow("002")]
    public void InspectWhenPartialLengthCannotHaveBoundedCompletionRefusesIt(string prefix)
    {
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(WindowsTransferRecoveryTestFixture.Mapping());
        byte[] record = WindowsTransferRecoveryTestFixture.Join(initial, Encoding.ASCII.GetBytes(prefix));

        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryCodec.Inspect(record));
    }

    private static string Corrupt(string json, string defect)
    {
        return defect switch
        {
            "version" => json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2", StringComparison.Ordinal),
            "versionType" => json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": \"1\"", StringComparison.Ordinal),
            "phase" => json.Replace("PreparedWithTargetPossiblyChanging", "DesiredContentVerified", StringComparison.Ordinal),
            "duplicateRoot" => json.Replace("\"schemaVersion\": 1,", "\"schemaVersion\": 1,\n\"schemaVersion\": 1,", StringComparison.Ordinal),
            "duplicateNested" => json.Replace("\"identity\": \"full-source\",", "\"identity\": \"full-source\",\n\"identity\": \"full-source\",", StringComparison.Ordinal),
            "unknownRoot" => json.Insert(1, "\n\"unknown\": 0,"),
            "unknownNested" => json.Replace("\"identity\": \"full-source\",", "\"identity\": \"full-source\",\n\"unknown\": 0,", StringComparison.Ordinal),
            "unknownContent" => json.Replace("\"length\": 3,", "\"length\": 3,\n\"unknown\": 0,", StringComparison.Ordinal),
            "missing" => json.Replace("\"identity\": \"full-source\",", "\"absent\": \"full-source\",", StringComparison.Ordinal),
            "null" => json.Replace("\"full-source\"", "null", StringComparison.Ordinal),
            "negative" => json.Replace("\"length\": 3", "\"length\": -3", StringComparison.Ordinal),
            "overflow" => json.Replace("\"length\": 3", "\"length\": 9223372036854775808", StringComparison.Ordinal),
            "lengthType" => json.Replace("\"length\": 3", "\"length\": \"3\"", StringComparison.Ordinal),
            "lengthFraction" => json.Replace("\"length\": 3", "\"length\": 3.0", StringComparison.Ordinal),
            "identityLength" => json.Replace("full-source", new string('x', 513), StringComparison.Ordinal),
            "badHash" => json.Replace(new string('A', 64), new string('a', 64), StringComparison.Ordinal),
            "hashLength" => json.Replace(new string('A', 64), new string('A', 63), StringComparison.Ordinal),
            "canonicalPath" => json.Replace(@"D:\\recovery\\source.txt", @"d:\\recovery\\source.txt", StringComparison.Ordinal),
            "pathAlias" => json.Replace(@"D:\\recovery\\source.txt", @"D:\\recovery\\target.txt", StringComparison.Ordinal),
            "objectAlias" => json.Replace("object-source", "object-target", StringComparison.Ordinal),
            "candidateCase" => json.Replace("NeNeCommander-", "NENECOMMANDER-", StringComparison.Ordinal),
            "candidateName" => json.Replace("-original.bin", "-other.bin", StringComparison.Ordinal),
            "unicode" => json.Replace("full-source", "\\uD800", StringComparison.Ordinal),
            "depth" => json.Replace("\"full-source\"", "{\"a\":{\"b\":{\"c\":{\"d\":0}}}}", StringComparison.Ordinal),
            "trailingJson" => json + "{}",
            "comments" => json.Insert(1, "/*comment*/"),
            "whitespace" => "\n" + json,
            "incompleteJson" => json[..^1],
            _ => throw new ArgumentOutOfRangeException(nameof(defect)),
        };
    }
}
