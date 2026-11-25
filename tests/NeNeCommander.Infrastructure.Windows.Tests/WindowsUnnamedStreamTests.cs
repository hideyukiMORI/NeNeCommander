using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves only complete single unnamed streams can satisfy the bounded observation.</summary>
[TestClass]
public sealed class WindowsUnnamedStreamTests
{
    /// <summary>Proves actual empty and populated default streams pass the exact bounded query.</summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("content")]
    public void RequireUnnamedStreamWhenFileHasOnlyDefaultStreamAcceptsItsLength(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("entry", content);
        using SafeFileHandle handle = WindowsFileIdentifier.OpenForAttributes(path);

        WindowsFileIdentifier.RequireUnnamedStream(handle, content.Length);

        _ = Assert.ThrowsExactly<IOException>(() => WindowsFileIdentifier.RequireUnnamedStream(handle, content.Length + 1));
    }

    /// <summary>Proves even an empty named stream refuses admission instead of being treated as no extra content.</summary>
    [TestMethod]
    [DataRow("Zone.Identifier", "")]
    [DataRow("Zone.Identifier", "zone")]
    [DataRow("custom", "value")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void RequireUnnamedStreamWhenNamedStreamExistsRefusesIt(string stream, string content)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("entry", "content");
        _ = root.WriteFile("entry:" + stream, content);
        using SafeFileHandle handle = WindowsFileIdentifier.OpenForAttributes(path);

        _ = Assert.ThrowsExactly<IOException>(() => WindowsFileIdentifier.RequireUnnamedStream(handle, 7));
    }

    /// <summary>Proves bounded native decoding rejects every incomplete or contradictory observation.</summary>
    [TestMethod]
    [DataRow("short")]
    [DataRow("excess")]
    [DataRow("next")]
    [DataRow("nameLength")]
    [DataRow("name")]
    [DataRow("size")]
    [DataRow("allocation")]
    [DataRow("negativeExpected")]
    public void MatchesWhenNativeObservationIsNotOneCompleteDefaultStreamRefusesIt(string defect)
    {
        byte[] bytes = Observation();
        Assert.IsTrue(WindowsUnnamedStream.Matches(bytes, 7));
        Mutate(bytes, defect);
        ReadOnlySpan<byte> observation = defect == "short" ? bytes.AsSpan(0, 39)
            : defect == "excess" ? new byte[41] : bytes;

        Assert.IsFalse(WindowsUnnamedStream.Matches(observation, defect == "negativeExpected" ? -1 : 7));
    }

    /// <summary>Proves invalid handles and absent buffers cannot authorize stream membership.</summary>
    [TestMethod]
    public void RequireUnnamedStreamWhenObservationCannotBeReadRefusesIt()
    {
        using SafeFileHandle invalid = new(IntPtr.Zero, false);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsFileIdentifier.RequireUnnamedStream(null!, 0));
        _ = Assert.ThrowsExactly<IOException>(() => WindowsFileIdentifier.RequireUnnamedStream(invalid, 0));
        Assert.IsFalse(WindowsUnnamedStream.Matches([], 0));
    }

    private static byte[] Observation()
    {
        byte[] bytes = new byte[WindowsUnnamedStream.ObservationLength];
        BitConverter.GetBytes(14u).CopyTo(bytes, 4);
        BitConverter.GetBytes(7L).CopyTo(bytes, 8);
        BitConverter.GetBytes(4096L).CopyTo(bytes, 16);
        Encoding.Unicode.GetBytes("::$DATA").CopyTo(bytes, 24);
        return bytes;
    }

    private static void Mutate(byte[] bytes, string defect)
    {
        switch (defect)
        {
            case "next":
                BitConverter.GetBytes(40u).CopyTo(bytes, 0);
                break;
            case "nameLength":
                BitConverter.GetBytes(uint.MaxValue).CopyTo(bytes, 4);
                break;
            case "name":
                bytes[24] = (byte)'x';
                break;
            case "size":
                BitConverter.GetBytes(8L).CopyTo(bytes, 8);
                break;
            case "allocation":
                BitConverter.GetBytes(-1L).CopyTo(bytes, 16);
                break;
            case "short":
            case "excess":
            case "negativeExpected":
                break;
            default:
                throw new ArgumentException("Unknown malformed observation case.", nameof(defect));
        }
    }
}
