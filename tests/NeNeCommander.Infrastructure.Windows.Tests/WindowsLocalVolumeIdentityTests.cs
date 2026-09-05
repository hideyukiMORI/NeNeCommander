using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves exact native character-buffer termination without invoking a second volume.</summary>
[TestClass]
public sealed class WindowsLocalVolumeIdentityTests
{
    /// <summary>Proves the first native null terminates text and an unterminated buffer retains every character.</summary>
    [TestMethod]
    public void ReadBufferUsesFirstNullOrCompleteBuffer()
    {
        Assert.AreEqual("", WindowsLocalVolumeIdentity.ReadBuffer(['\0', 'x']));
        Assert.AreEqual("a", WindowsLocalVolumeIdentity.ReadBuffer(['a', '\0', 'x']));
        Assert.AreEqual("ab", WindowsLocalVolumeIdentity.ReadBuffer(['a', 'b']));
    }
}
