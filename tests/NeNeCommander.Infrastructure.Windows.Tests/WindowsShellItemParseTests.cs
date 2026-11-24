using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Launching;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Exercises actual native PIDL parsing and release on owned paths without opening Explorer.</summary>
[TestClass]
public sealed class WindowsShellItemParseTests
{
    /// <summary>Proves the native Unicode ABI returns an owned absolute PIDL on the existing STA.</summary>
    [TestMethod]
    public async Task ParseWhenOwnedUnicodeFileExistsReturnsReleasableAbsolutePidlAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.CreateFile("A & 漢字.txt");
        WindowsLocalIoExecutionBoundary boundary = new();

        _ = await boundary.ExecuteShellAsync(() =>
        {
            WindowsShellItemParseResult parsed = WindowsShellItemSelection.Parse(path);
            try
            {
                Assert.AreEqual(0, parsed.HResult);
                Assert.AreNotEqual(IntPtr.Zero, parsed.ItemIdList);
                Assert.IsTrue(Marshal.ReadInt16(parsed.ItemIdList) > 0);
                return 0;
            }
            finally
            {
                if (parsed.ItemIdList != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(parsed.ItemIdList);
                }
            }
        });
    }
}
