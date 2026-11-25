using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves literal text semantics independently of filesystem identity and UI input.</summary>
[TestClass]
public sealed class PaneNameFilterQueryTests
{
    /// <summary>Proves all separated terms match anywhere in the complete name.</summary>
    [TestMethod]
    [DataRow("report .TXT", "REPORT-final.txt")]
    [DataRow("\t report\u2003final \r\n", "Final Report.txt")]
    [DataRow("", "anything")]
    [DataRow(" \t\u2003", "anything")]
    [DataRow("a a", "a")]
    [DataRow("[a]", "literal[a].txt")]
    [DataRow("*", "literal*.txt")]
    [DataRow("?", "literal?.txt")]
    [DataRow("\uD800", "literal\uD800.txt")]
    public void MatchesWhenEveryLiteralTermOccursAcceptsExactText(string text, string name)
    {
        PaneNameFilterQuery query = PaneNameFilterQuery.Parse(text);

        Assert.IsTrue(query.Matches(name));
        Assert.AreSame(text, query.Text);
    }

    /// <summary>Proves no wildcard, normalization, path or width/kana interpretation is added.</summary>
    [TestMethod]
    [DataRow("report absent", "report.txt")]
    [DataRow("*.txt", "report.txt")]
    [DataRow("?.txt", "a.txt")]
    [DataRow("é", "e\u0301.txt")]
    [DataRow("A", "Ａ.txt")]
    [DataRow("か", "カ.txt")]
    [DataRow("folder/file", "file")]
    public void MatchesWhenLiteralTermIsMissingRejectsName(string text, string name)
    {
        PaneNameFilterQuery query = PaneNameFilterQuery.Parse(text);

        Assert.IsFalse(query.Matches(name));
    }

    /// <summary>Proves whitespace-only queries retain their exact text while imposing no restriction.</summary>
    [TestMethod]
    public void ParseWhenTextHasNoTermsReturnsUnrestrictedOwnedValue()
    {
        PaneNameFilterQuery query = PaneNameFilterQuery.Parse(" \t\u2003");

        Assert.IsTrue(query.IsEmpty);
        Assert.IsTrue(PaneNameFilterQuery.Empty.IsEmpty);
        Assert.IsFalse(PaneNameFilterQuery.Parse("a").IsEmpty);
        Assert.AreEqual(" \t\u2003", query.Text);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneNameFilterQuery.Parse(null!));
    }
}
