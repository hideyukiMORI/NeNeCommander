using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves the strict settings validator reports the first precise failure kind when exactly one
/// part of an otherwise shaped document is invalid.
/// </summary>
[TestClass]
public sealed class SettingsDocumentFailureKindTests
{
    private const string Order = "{\"key\":\"name\",\"direction\":\"ascending\"}";

    /// <summary>Proves text that cannot be encoded as strict UTF-8 is malformed rather than repaired.</summary>
    [TestMethod]
    public void ValidateWhenTextContainsLoneSurrogateRejectsMalformed()
    {
        string input = "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\uD800\"}";

        SettingsReadOutcome outcome = SettingsDocumentValidator.Validate(input);

        Assert.AreSame(SettingsReadFailureKind.Malformed, Rejected(outcome));
    }

    /// <summary>Proves the byte entry point bounds oversized whitespace before classifying it as empty.</summary>
    [TestMethod]
    public void ValidateWhenBytesExceedMaximumRejectsTooLarge()
    {
        byte[] input = [.. Enumerable.Repeat((byte)0x20, SettingsDocumentValidator.MaximumDocumentLength + 1)];

        SettingsReadOutcome outcome = SettingsDocumentValidator.Validate(new ReadOnlyMemory<byte>(input));

        Assert.AreSame(SettingsReadFailureKind.TooLarge, Rejected(outcome));
    }

    /// <summary>Proves a conflicting duplicate version is reported before any missing root member.</summary>
    /// <param name="input">Incomplete document whose version members conflict.</param>
    [TestMethod]
    [DataRow("{\"schemaVersion\":1,\"schemaVersion\":2}")]
    [DataRow("{\"schemaVersion\":1,\"schemaVersion\":\"1\"}")]
    public void ValidateWhenVersionDuplicatesConflictRejectsUnexpectedProperty(string input)
    {
        SettingsReadOutcome outcome = SettingsDocumentValidator.Validate(input);

        Assert.AreSame(SettingsReadFailureKind.UnexpectedProperty, Rejected(outcome));
    }

    /// <summary>Proves a non-array bookmark list is malformed even when the category list is oversized.</summary>
    [TestMethod]
    public void ValidateWhenBookmarksAreNotArrayRejectsMalformedBeforeCount()
    {
        string input = "{\"schemaVersion\":2,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\",\"bookmarkCategories\":[" +
            Repeat("\"Work\"", 33) + "],\"bookmarks\":\"bookmarks\"}";

        SettingsReadOutcome outcome = SettingsDocumentValidator.Validate(input);

        Assert.AreSame(SettingsReadFailureKind.Malformed, Rejected(outcome));
    }

    /// <summary>Proves either oversized list alone is rejected before its elements are read.</summary>
    /// <param name="categoryCount">Number of non-string categories.</param>
    /// <param name="bookmarkCount">Number of non-object bookmarks.</param>
    [TestMethod]
    [DataRow(33, 0)]
    [DataRow(0, 129)]
    public void ValidateWhenOneListExceedsItsCountRejectsInvalidBookmarks(int categoryCount, int bookmarkCount)
    {
        string input = CatalogDocument(Repeat("0", categoryCount), Repeat("0", bookmarkCount));

        SettingsReadOutcome outcome = SettingsDocumentValidator.Validate(input);

        Assert.AreSame(SettingsReadFailureKind.InvalidBookmarks, Rejected(outcome));
    }

    /// <summary>Proves a null name or path is a type failure even when the other member is a string.</summary>
    /// <param name="bookmark">Bookmark with exactly one non-string text member.</param>
    [TestMethod]
    [DataRow("{\"name\":null,\"path\":\"C:\\\\saved\",\"category\":null,\"shortcutSlot\":null}")]
    [DataRow("{\"name\":\"Saved\",\"path\":null,\"category\":null,\"shortcutSlot\":null}")]
    public void ValidateWhenOneBookmarkTextIsNullRejectsMalformed(string bookmark)
    {
        string input = CatalogDocument(string.Empty, bookmark);

        SettingsReadOutcome outcome = SettingsDocumentValidator.Validate(input);

        Assert.AreSame(SettingsReadFailureKind.Malformed, Rejected(outcome));
    }

    /// <summary>Proves the category failure wins when both the category and the slot are invalid.</summary>
    /// <param name="category">Invalid category value.</param>
    /// <param name="slot">Invalid slot value.</param>
    /// <param name="expected">Name of the category failure kind.</param>
    [TestMethod]
    [DataRow("5", "99", "Malformed")]
    [DataRow("\"\"", "\"one\"", "InvalidBookmarks")]
    public void ValidateWhenCategoryAndSlotAreInvalidReportsCategoryFailure(string category, string slot, string expected)
    {
        string bookmark = "{\"name\":\"Saved\",\"path\":\"C:\\\\saved\",\"category\":" + category +
            ",\"shortcutSlot\":" + slot + "}";

        SettingsReadOutcome outcome = SettingsDocumentValidator.Validate(CatalogDocument(string.Empty, bookmark));

        Assert.AreSame(Kind(expected), Rejected(outcome));
    }

    /// <summary>Proves an invalid right sort order keeps its own failure kind when the left order is valid.</summary>
    /// <param name="right">Right order with one shape failure.</param>
    /// <param name="expected">Name of the right-side failure kind.</param>
    [TestMethod]
    [DataRow("{}", "Incomplete")]
    [DataRow("{\"key\":\"name\",\"direction\":\"ascending\",\"extra\":0}", "UnexpectedProperty")]
    public void ValidateWhenOnlyRightSortOrderIsInvalidReportsRightFailure(string right, string expected)
    {
        string input = "{\"schemaVersion\":3,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[],\"paneSortOrders\":{\"left\":" + Order +
            ",\"right\":" + right + "}}";

        SettingsReadOutcome outcome = SettingsDocumentValidator.Validate(input);

        Assert.AreSame(Kind(expected), Rejected(outcome));
    }

    private static string CatalogDocument(string categories, string bookmarks)
    {
        return "{\"schemaVersion\":2,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\",\"bookmarkCategories\":[" +
            categories + "],\"bookmarks\":[" + bookmarks + "]}";
    }

    private static string Repeat(string element, int count)
    {
        return string.Join(',', Enumerable.Repeat(element, count));
    }

    private static SettingsReadFailureKind Rejected(SettingsReadOutcome outcome)
    {
        return Assert.IsInstanceOfType<SettingsRejected>(outcome).Kind;
    }

    private static SettingsReadFailureKind Kind(string name)
    {
        return name switch
        {
            "Malformed" => SettingsReadFailureKind.Malformed,
            "InvalidBookmarks" => SettingsReadFailureKind.InvalidBookmarks,
            "Incomplete" => SettingsReadFailureKind.Incomplete,
            "UnexpectedProperty" => SettingsReadFailureKind.UnexpectedProperty,
            _ => throw new AssertFailedException("The failure kind is not declared."),
        };
    }
}
