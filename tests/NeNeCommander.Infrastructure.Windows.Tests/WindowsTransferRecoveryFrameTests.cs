using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the exact binary envelope and its independent bounds before schema interpretation.</summary>
[TestClass]
public sealed class WindowsTransferRecoveryFrameTests
{
    /// <summary>Proves the fixed header, payload terminator and whole-frame evidence against a known digest.</summary>
    [TestMethod]
    public void WrapWhenPayloadIsKnownMatchesExactEnvelope()
    {
        byte[] frame = WindowsTransferRecoveryFrame.Wrap("abc"u8);
        const string expected = "00000003:BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD\nabc\n";
        WindowsTransferRecoveryFrame parsed = WindowsTransferRecoveryFrame.Parse(frame);

        Assert.AreEqual(expected, Encoding.UTF8.GetString(frame));
        Assert.AreEqual("abc", parsed.Json);
        Assert.AreEqual(frame.Length, parsed.Evidence.Size.Bytes);
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(frame)), parsed.Evidence.Hash.Value);
    }

    /// <summary>Proves the payload accepts its maximum and rejects zero or one extra byte.</summary>
    [TestMethod]
    public void WrapWhenPayloadTouchesBoundsAdmitsOnlyValidLengths()
    {
        byte[] maximum = new byte[WindowsTransferRecoveryFrame.MaximumPayloadLength];
        Array.Fill(maximum, (byte)'a');
        byte[] frame = WindowsTransferRecoveryFrame.Wrap(maximum);

        Assert.AreEqual(WindowsTransferRecoveryFrame.MaximumFrameLength, frame.Length);
        Assert.AreEqual(maximum.Length, WindowsTransferRecoveryFrame.Parse(frame).Json.Length);
        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryFrame.Wrap([]));
        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryFrame.Wrap(new byte[maximum.Length + 1]));
    }

    /// <summary>Proves malformed or oversized available header prefixes cannot be dismissed as harmless truncation.</summary>
    [TestMethod]
    [DataRow("X")]
    [DataRow("0000000a")]
    [DataRow("00000000")]
    [DataRow("00100001")]
    [DataRow("FFFFFFFF")]
    [DataRow("00000003;")]
    public void RequireHeaderPrefixWhenKnownBytesAreInvalidRefusesThem(string prefix)
    {
        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryFrame.RequireHeaderPrefix(Encoding.ASCII.GetBytes(prefix)));
    }

    /// <summary>Proves complete frames reject checksum, newline and strict-UTF8 defects.</summary>
    [TestMethod]
    [DataRow("checksum")]
    [DataRow("headerNewline")]
    [DataRow("terminator")]
    [DataRow("utf8")]
    public void ParseWhenCompleteFrameIsCorruptRefusesIt(string defect)
    {
        byte[] frame = defect == "utf8" ? WindowsTransferRecoveryFrame.Wrap([0xC0, 0xAF]) : WindowsTransferRecoveryFrame.Wrap("abc"u8);
        if (defect != "utf8")
        {
            int position = defect == "checksum" ? 9 : defect == "headerNewline" ? 73 : frame.Length - 1;
            frame[position] = (byte)'0';
        }

        _ = Assert.ThrowsExactly<InvalidDataException>(() => WindowsTransferRecoveryFrame.Parse(frame));
    }
}
