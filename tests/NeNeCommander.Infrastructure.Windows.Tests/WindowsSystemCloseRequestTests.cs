using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.Windowing;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the system close request never reaches a queue other than a named window's.</summary>
[TestClass]
public sealed class WindowsSystemCloseRequestTests
{
    /// <summary>
    /// A zero handle would post the close command to the calling thread's own queue, so it is
    /// refused before any native call. No real handle is posted to: a test must not close a window.
    /// </summary>
    [TestMethod]
    public void PostWhenHandleIsZeroRefusesBeforeNativeCall()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WindowsSystemCloseRequest.Post(0));
    }
}
