using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Checks the ADR-0095 version-one wire contract independently of its own decoder and canonical reserializer.</summary>
[TestClass]
public sealed class WindowsTransferRecoverySchemaTests
{
    /// <summary>The saved initial payload exposes every named original object and content observation to an independent JSON reader.</summary>
    [TestMethod]
    public void EncodeInitialWhenMappingIsValidWritesCompleteVersionOneSchema()
    {
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping();
        byte[] encoded = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        using JsonDocument document = ParseIndependentFrame(encoded);
        JsonElement root = document.RootElement;

        RequireProperties(root, ["schemaVersion", "phase", "operation", "source", "target", "backup", "prepared", "original", "desired"]);
        Assert.AreEqual(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.AreEqual("PreparedWithTargetPossiblyChanging", root.GetProperty("phase").GetString());
        Assert.AreEqual("0123456789abcdef0123456789abcdef", root.GetProperty("operation").GetString());
        RequireEntry(root.GetProperty("source"), mapping.Files.Source);
        RequireEntry(root.GetProperty("target"), mapping.Files.Target);
        RequireEntry(root.GetProperty("backup"), mapping.Files.Backup);
        RequireEntry(root.GetProperty("prepared"), mapping.Files.Prepared);
        RequireContent(root.GetProperty("original"), mapping.Original);
        RequireContent(root.GetProperty("desired"), mapping.Desired);
    }

    /// <summary>The append carries an explicit operation, complete initial-frame digest and exact desired observation.</summary>
    [TestMethod]
    public void EncodeCompletionWhenInitialIsValidWritesBoundVersionOneSchema()
    {
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping();
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        byte[] append = WindowsTransferRecoveryCodec.EncodeCompletion(initial);
        using JsonDocument document = ParseIndependentFrame(append);
        JsonElement root = document.RootElement;

        RequireProperties(root, ["schemaVersion", "phase", "operation", "initialFrameSha256", "targetObjectIdentity", "desired"]);
        Assert.AreEqual(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.AreEqual("DesiredContentVerified", root.GetProperty("phase").GetString());
        Assert.AreEqual("0123456789abcdef0123456789abcdef", root.GetProperty("operation").GetString());
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(initial)), root.GetProperty("initialFrameSha256").GetString());
        Assert.AreEqual(mapping.Files.Target.ObjectIdentity.Value, root.GetProperty("targetObjectIdentity").GetString());
        RequireContent(root.GetProperty("desired"), mapping.Desired);
    }

    private static JsonDocument ParseIndependentFrame(byte[] frame)
    {
        Assert.IsGreaterThan(75, frame.Length);
        Assert.AreEqual((byte)':', frame[8]);
        Assert.AreEqual((byte)'\n', frame[73]);
        Assert.AreEqual((byte)'\n', frame[^1]);
        int length = int.Parse(Encoding.ASCII.GetString(frame.AsSpan(0, 8)), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        Assert.AreEqual(frame.Length - 75, length);
        Assert.AreEqual(length.ToString("X8", CultureInfo.InvariantCulture), Encoding.ASCII.GetString(frame.AsSpan(0, 8)));
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(frame.AsSpan(74, length))), Encoding.ASCII.GetString(frame.AsSpan(9, 64)));
        return JsonDocument.Parse(frame.AsMemory(74, length));
    }

    private static void RequireEntry(JsonElement entry, VerifiedEntrySnapshot expected)
    {
        RequireProperties(entry, ["path", "identity", "objectIdentity"]);
        Assert.AreEqual(expected.Path.CanonicalText, entry.GetProperty("path").GetString());
        Assert.AreEqual(expected.Identity.Value, entry.GetProperty("identity").GetString());
        Assert.AreEqual(expected.ObjectIdentity.Value, entry.GetProperty("objectIdentity").GetString());
    }

    private static void RequireContent(JsonElement content, FileContentEvidence expected)
    {
        RequireProperties(content, ["length", "sha256"]);
        Assert.AreEqual(expected.Size.Bytes, content.GetProperty("length").GetInt64());
        Assert.AreEqual(expected.Hash.Value, content.GetProperty("sha256").GetString());
    }

    private static void RequireProperties(JsonElement element, string[] names)
    {
        CollectionAssert.AreEqual(names, element.EnumerateObject().Select(property => property.Name).ToArray());
    }
}
