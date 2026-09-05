using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Wsl;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves successful catalog outcomes expose only unique WSL roots.</summary>
[TestClass]
public sealed class WslDistributionCatalogOutcomeTests
{
    /// <summary>Proves a valid root sequence is copied in order.</summary>
    [TestMethod]
    public void SucceededWhenRootsAreValidReturnsOwnedOrderedSnapshot()
    {
        List<WslPath> roots =
        [
            RequireWsl("\\\\wsl.localhost\\Ubuntu"),
            RequireWsl("\\\\wsl.localhost\\Debian"),
        ];

        WslDistributionCatalogSucceeded outcome = Assert.IsInstanceOfType<WslDistributionCatalogSucceeded>(
            WslDistributionCatalogOutcome.Succeeded(roots));
        roots.Clear();

        Assert.HasCount(2, outcome.Roots);
        Assert.AreEqual("Ubuntu", outcome.Roots[0].DistributionName);
        Assert.AreEqual("Debian", outcome.Roots[1].DistributionName);
    }

    /// <summary>Proves null, child, duplicate, and null-entry snapshots are rejected.</summary>
    [TestMethod]
    public void SucceededWhenRootsAreInvalidRejectsSnapshot()
    {
        WslPath root = RequireWsl("\\\\wsl.localhost\\Ubuntu");
        WslPath duplicate = RequireWsl("\\\\wsl.localhost\\ubuntu");
        WslPath child = RequireWsl("\\\\wsl.localhost\\Ubuntu\\home");
        IReadOnlyList<WslPath> nullEntry = [null!];

        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WslDistributionCatalogOutcome.Succeeded(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WslDistributionCatalogOutcome.Succeeded(nullEntry));
        ArgumentException childException = Assert.ThrowsExactly<ArgumentException>(
            () => WslDistributionCatalogOutcome.Succeeded([child]));
        _ = Assert.ThrowsExactly<ArgumentException>(
            () => WslDistributionCatalogOutcome.Succeeded([root, duplicate]));

        StringAssert.Contains(childException.Message, "Distribution roots must be unique WSL roots.");
    }

    /// <summary>Proves the failed outcome rejects an absent failure kind.</summary>
    [TestMethod]
    public void FailedWhenFailureIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WslDistributionCatalogOutcome.Failed(null!));
    }

    /// <summary>Proves cancellation and failure factories preserve their closed variants.</summary>
    [TestMethod]
    public void CancelledAndFailedReturnClosedVariants()
    {
        _ = Assert.IsInstanceOfType<WslDistributionCatalogCancelled>(
            WslDistributionCatalogOutcome.Cancelled());
        WslDistributionCatalogFailed failed = Assert.IsInstanceOfType<WslDistributionCatalogFailed>(
            WslDistributionCatalogOutcome.Failed(WslDistributionCatalogFailureKind.ProviderUnavailable));

        Assert.AreSame(WslDistributionCatalogFailureKind.ProviderUnavailable, failed.Failure);
    }

    private static WslPath RequireWsl(string text)
    {
        PathParseSuccess parsed = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text));
        return Assert.IsInstanceOfType<WslPath>(parsed.Path);
    }
}
