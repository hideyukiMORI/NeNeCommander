using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Launching;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Launching;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves PIDL ownership and phase-qualified cancellation without displaying Explorer.</summary>
[TestClass]
public sealed class WindowsShellItemSelectionTests
{
    /// <summary>Proves one exact canonical parse, one handoff and one release on accepted work.</summary>
    [TestMethod]
    public void RevealWhenAcceptedParsesExactTargetAndReleasesOnce()
    {
        FileSystemPath target = Parse("C:\\folder space\\a&b.txt");
        IntPtr owned = new(37);
        int parses = 0;
        int selections = 0;
        int frees = 0;
        WindowsShellItemSelection selection = new(path =>
        {
            parses++;
            Assert.AreEqual(target.CanonicalText, path);
            return new WindowsShellItemParseResult(0, owned);
        }, pointer =>
        {
            selections++;
            Assert.AreEqual(owned, pointer);
            Assert.AreEqual(0, frees);
            return 0;
        }, pointer =>
        {
            frees++;
            Assert.AreEqual(owned, pointer);
        });

        FileLaunchOutcome outcome = selection.Reveal(target, CancellationToken.None);

        _ = Assert.IsInstanceOfType<FileLaunchAccepted>(outcome);
        Assert.AreEqual(1, parses);
        Assert.AreEqual(1, selections);
        Assert.AreEqual(1, frees);
    }

    /// <summary>Proves HRESULT failure releases even an unexpected non-null native allocation.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(37)]
    public void RevealWhenParseFailsNeverSelectsAndReleasesOnlyOwnedPointer(int allocation)
    {
        int frees = 0;
        WindowsShellItemSelection selection = new(
            _ => new WindowsShellItemParseResult(unchecked((int)0x80070002), new IntPtr(allocation)),
            _ => throw new AssertFailedException("Failed parsing cannot select."),
            _ => frees++);

        FileLaunchFailed failed = Assert.IsInstanceOfType<FileLaunchFailed>(
            selection.Reveal(Parse("C:\\missing.txt"), CancellationToken.None));

        Assert.AreSame(FileLaunchFailureKind.NotFound, failed.Failure);
        Assert.AreEqual(allocation == 0 ? 0 : 1, frees);
    }

    /// <summary>Proves a malformed successful parse cannot hand a null PIDL to the Shell.</summary>
    [TestMethod]
    public void RevealWhenSuccessfulParseHasNoPointerRejectsWithoutSelectionOrFree()
    {
        WindowsShellItemSelection selection = new(
            _ => new WindowsShellItemParseResult(0, IntPtr.Zero),
            _ => throw new AssertFailedException("A missing PIDL cannot select."),
            _ => throw new AssertFailedException("No allocation may be freed."));

        FileLaunchFailed failed = Assert.IsInstanceOfType<FileLaunchFailed>(
            selection.Reveal(Parse("C:\\item.txt"), CancellationToken.None));

        Assert.AreSame(FileLaunchFailureKind.ShellRejected, failed.Failure);
    }

    /// <summary>Proves cancellation after parsing frees the PIDL and prevents the final call.</summary>
    [TestMethod]
    public void RevealWhenCancelledAfterParseReleasesWithoutSelection()
    {
        using CancellationTokenSource cancellation = new();
        int frees = 0;
        WindowsShellItemSelection selection = new(_ =>
        {
            cancellation.Cancel();
            return new WindowsShellItemParseResult(0, new IntPtr(37));
        }, _ => throw new AssertFailedException("Cancelled parsing cannot select."), _ => frees++);

        FileLaunchOutcome outcome = selection.Reveal(Parse("C:\\item.txt"), cancellation.Token);

        _ = Assert.IsInstanceOfType<FileLaunchCancelled>(outcome);
        Assert.AreEqual(1, frees);
    }

    /// <summary>Proves accepted work remains accepted when cancellation arrives inside final handoff.</summary>
    [TestMethod]
    public void RevealWhenCancelledDuringAcceptedSelectionRemainsAcceptedAndReleases()
    {
        using CancellationTokenSource cancellation = new();
        int frees = 0;
        WindowsShellItemSelection selection = new(
            _ => new WindowsShellItemParseResult(0, new IntPtr(37)),
            _ =>
            {
                cancellation.Cancel();
                return 0;
            }, _ => frees++);

        FileLaunchOutcome outcome = selection.Reveal(Parse("C:\\item.txt"), cancellation.Token);

        _ = Assert.IsInstanceOfType<FileLaunchAccepted>(outcome);
        Assert.AreEqual(1, frees);
    }

    /// <summary>Proves unexpected defects propagate while the finally block releases exactly once.</summary>
    [TestMethod]
    public void RevealWhenSelectionThrowsReleasesAndPreservesDefect()
    {
        InvalidOperationException expected = new("Selection defect.");
        int frees = 0;
        WindowsShellItemSelection selection = new(
            _ => new WindowsShellItemParseResult(0, new IntPtr(37)),
            _ => throw expected, _ => frees++);

        InvalidOperationException actual = Assert.ThrowsExactly<InvalidOperationException>(
            () => selection.Reveal(Parse("C:\\item.txt"), CancellationToken.None));

        Assert.AreSame(expected, actual);
        Assert.AreEqual(1, frees);
    }

    /// <summary>Proves a Shell rejection never retries and maps only genuine Win32 HRESULTs.</summary>
    [TestMethod]
    [DataRow(unchecked((int)0x80070002), "missing")]
    [DataRow(unchecked((int)0x80070003), "missing")]
    [DataRow(unchecked((int)0x80070005), "denied")]
    [DataRow(unchecked((int)0x8007052E), "denied")]
    [DataRow(unchecked((int)0x80070035), "provider")]
    [DataRow(unchecked((int)0x80070483), "association")]
    [DataRow(unchecked((int)0x80004005), "shell")]
    [DataRow(unchecked((int)0x80040005), "shell")]
    public void RevealWhenSelectionRejectsNormalizesFailureAndReleases(int result, string kind)
    {
        int calls = 0;
        int frees = 0;
        WindowsShellItemSelection selection = new(
            _ => new WindowsShellItemParseResult(0, new IntPtr(37)),
            _ =>
            {
                calls++;
                return result;
            }, _ => frees++);

        FileLaunchFailed failed = Assert.IsInstanceOfType<FileLaunchFailed>(
            selection.Reveal(Parse("C:\\item.txt"), CancellationToken.None));

        FileLaunchFailureKind expected = kind switch
        {
            "missing" => FileLaunchFailureKind.NotFound,
            "denied" => FileLaunchFailureKind.AccessDenied,
            "provider" => FileLaunchFailureKind.ProviderUnavailable,
            "association" => FileLaunchFailureKind.AssociationUnavailable,
            _ => FileLaunchFailureKind.ShellRejected,
        };
        Assert.AreSame(expected, failed.Failure);
        Assert.AreEqual(1, calls);
        Assert.AreEqual(1, frees);
    }

    private static FileSystemPath Parse(string path)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path;
    }
}
