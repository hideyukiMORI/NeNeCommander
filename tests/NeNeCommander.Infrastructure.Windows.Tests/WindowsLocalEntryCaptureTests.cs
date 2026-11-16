using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves verification capture observes the entry as it is now, not a stale cached view.</summary>
[TestClass]
public sealed class WindowsLocalEntryCaptureTests
{
    /// <summary>Proves a capture from an entry cached before a rewrite matches a capture from a fresh entry.</summary>
    [TestMethod]
    public void CaptureWhenEntryWasCachedBeforeRewriteObservesCurrentContent()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string text = root.WriteFile("grown.txt", "a");
        WindowsLocalPath path = Assert.IsInstanceOfType<WindowsLocalPath>(WindowsLocalOperationTestContext.ParsePath(text));
        FileInfo cached = new(text);
        _ = cached.Length;
        File.AppendAllText(text, "bbbb");

        VerifiedEntrySnapshot captured = WindowsLocalEntryIdentity.Capture(path, cached);

        Assert.AreEqual(WindowsLocalEntryIdentity.Capture(path, new FileInfo(text)).Identity, captured.Identity);
    }
}
