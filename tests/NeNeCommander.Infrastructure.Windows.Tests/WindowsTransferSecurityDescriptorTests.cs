using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the supplied native descriptor explicitly requests protected current-user ownership before filesystem defaults can apply.</summary>
[TestClass]
public sealed class WindowsTransferSecurityDescriptorTests
{
    /// <summary>The native request itself has one noninherited full-control user grant and an explicit protected DACL.</summary>
    [TestMethod]
    public void CaptureCurrentWhenDescriptorIsInspectedContainsExplicitProtectedUserOnlyRequest()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        using WindowsTransferSecurityDescriptor descriptor = WindowsTransferSecurityDescriptor.CaptureCurrent();
        int length = descriptor.ByteLength;
        Assert.IsGreaterThan(0, length);
        Assert.IsLessThanOrEqualTo(65536, length);
        byte[] bytes = new byte[length];
        Marshal.Copy(descriptor.DangerousGetHandle(), bytes, 0, length);
        RawSecurityDescriptor supplied = new(bytes, 0);

        Assert.AreEqual(identity.User, supplied.Owner);
        Assert.IsTrue((supplied.ControlFlags & ControlFlags.SelfRelative) != 0);
        Assert.IsTrue((supplied.ControlFlags & ControlFlags.DiscretionaryAclProtected) != 0);
        Assert.IsNotNull(supplied.DiscretionaryAcl);
        Assert.HasCount(1, supplied.DiscretionaryAcl);
        CommonAce grant = Assert.IsInstanceOfType<CommonAce>(supplied.DiscretionaryAcl[0]);
        Assert.AreEqual(identity.User, grant.SecurityIdentifier);
        Assert.AreEqual(AceQualifier.AccessAllowed, grant.AceQualifier);
        Assert.AreEqual((int)FileSystemRights.FullControl, grant.AccessMask);
        Assert.AreEqual(AceFlags.None, grant.AceFlags);
        Assert.IsFalse(descriptor.IsClosed);
    }

}
