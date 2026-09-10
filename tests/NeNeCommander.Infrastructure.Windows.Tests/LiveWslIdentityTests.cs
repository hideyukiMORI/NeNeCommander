using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Defines the ADR-0049 live read-only identity assertions on a real distribution.</summary>
[TestClass]
[DoNotParallelize]
public sealed class LiveWslIdentityTests
{
    private const int InodeField = 2;
    private const int LinkCountField = 5;
    private const int ChangeTimeField = 8;
    private const int TokenFieldCount = 9;

    /// <summary>Gets the MSTest context carrying the ephemeral opt-in root parameter.</summary>
    public TestContext? TestContext { get; set; }

    /// <summary>
    /// Requires the inode, hard-link count, and change time inside one owned fixture's `wsl-v2`
    /// identity to equal the values a read-only `stat` reports inside the distribution.
    /// </summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("LiveWsl")]
    public async Task FindWhenLiveOwnedFixtureIsInspectedMatchesDistributionStatAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            _ = root.CreateDirectory("stat-source");
            _ = root.WriteFile("stat-source/payload.bin", [17, 23, 0, 255]);

            RequireStatAgreement(context, root, "stat-source/payload.bin");
            RequireStatAgreement(context, root, "stat-source");
        }
        finally
        {
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    /// <summary>Requires an owned symbolic link and its target to produce different identities.</summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("LiveWsl")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-020")]
    public async Task FindWhenLiveOwnedLinkIsInspectedDiffersFromItsTargetAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            _ = root.WriteFile("link-target.bin", [5, 8, 13]);
            _ = root.CreateFileSymbolicLink("entry-link.bin", "link-target.bin");

            string target = root.ReadIdentity("link-target.bin");
            string link = root.ReadIdentity("entry-link.bin");

            context.WriteLine("LiveWsl identity=link-versus-target provider=Wsl root=redacted identity=redacted");
            Assert.AreNotEqual(target, link);
            Assert.AreNotEqual(Field(target, InodeField), Field(link, InodeField));
            Assert.AreEqual("link", Field(link, 3));
            Assert.AreEqual("file", Field(target, 3));
        }
        finally
        {
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    /// <summary>Requires an unchanged owned fixture to repeat one identity across an intervening read.</summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("LiveWsl")]
    public async Task FindWhenLiveOwnedFixtureIsReadAgainRepeatsTheSameIdentityAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            _ = root.WriteFile("stable.bin", [2, 3, 5, 7, 11]);

            string first = root.ReadIdentity("stable.bin");
            CollectionAssert.AreEqual(new byte[] { 2, 3, 5, 7, 11 }, root.ReadFile("stable.bin"));
            string second = root.ReadIdentity("stable.bin");

            context.WriteLine("LiveWsl identity=stable-across-read provider=Wsl root=redacted identity=redacted");
            Assert.AreEqual(first, second);
        }
        finally
        {
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    private static void RequireStatAgreement(TestContext context, LiveWslTestRoot root, string relativePath)
    {
        WslPath path = root.OwnedPath(relativePath);
        string identity = root.ReadIdentity(relativePath);
        LiveWslStatFact fact = LiveWslStatFact.Read(path);

        context.WriteLine("LiveWsl identity=stat-agreement provider=Wsl root=redacted identity=redacted");
        Assert.AreEqual(fact.Inode, Number(identity, InodeField));
        Assert.AreEqual(fact.LinkCount, Number(identity, LinkCountField));
        Assert.AreEqual(
            fact.ChangeSeconds,
            DateTimeOffset.FromFileTime(Number(identity, ChangeTimeField)).ToUnixTimeSeconds());
    }

    private static long Number(string identity, int field)
    {
        return long.Parse(Field(identity, field), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    }

    private static string Field(string identity, int field)
    {
        string[] fields = identity.Split('|');
        return fields.Length == TokenFieldCount
            ? fields[field]
            : throw new InvalidOperationException("The live identity token has an unexpected shape.");
    }

    private TestContext RequireContext()
    {
        return TestContext ?? throw new InvalidOperationException("MSTest did not provide TestContext.");
    }
}
