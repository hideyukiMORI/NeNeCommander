using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// Owns strict, bounded validation of every supported persisted settings schema and is the sole
/// place where untrusted UTF-8 settings bytes become typed <see cref="UserSettings"/>.
/// </summary>
public static class SettingsDocumentValidator
{
    /// <summary>Maximum accepted UTF-8 document length in bytes.</summary>
    public const int MaximumDocumentLength = 65536;

    private const int LegacySchemaVersion = 1;
    private const int SupportedSchemaVersion = 2;
    private const string SchemaVersionName = "schemaVersion";
    private const string HiddenItemsName = "showHiddenItems";
    private const string ColorSchemeName = "colorScheme";
    private const string CategoriesName = "bookmarkCategories";
    private const string BookmarksName = "bookmarks";
    private const string BookmarkName = "name";
    private const string BookmarkPathName = "path";
    private const string BookmarkCategoryPropertyName = "category";
    private const string BookmarkSlotName = "shortcutSlot";
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    /// <summary>Validates untrusted settings text without applying partial state.</summary>
    /// <param name="input">Untrusted persisted JSON text.</param>
    /// <returns>Complete settings or a typed rejection.</returns>
    public static SettingsReadOutcome Validate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Empty);
        }

        try
        {
            int byteCount = StrictUtf8.GetByteCount(input);
            return byteCount > MaximumDocumentLength
                ? SettingsReadOutcome.Rejected(SettingsReadFailureKind.TooLarge)
                : Validate(StrictUtf8.GetBytes(input));
        }
        catch (EncoderFallbackException)
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed);
        }
    }

    internal static SettingsReadOutcome Validate(ReadOnlyMemory<byte> input)
    {
        if (input.Length > MaximumDocumentLength)
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.TooLarge);
        }

        ReadOnlyMemory<byte> content = WithoutUtf8Preamble(input);
        if (IsJsonWhitespace(content.Span))
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Empty);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            return ValidateRoot(document.RootElement);
        }
        catch (JsonException)
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed);
        }
        catch (InvalidOperationException)
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed);
        }
    }

    private static SettingsReadOutcome ValidateRoot(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed);
        }

        SettingsReadFailureKind? versionFailure = ReadVersion(root, out int version);
        return versionFailure is not null
            ? SettingsReadOutcome.Rejected(versionFailure)
            : version == LegacySchemaVersion ? ReadVersionOne(root) : ReadVersionTwo(root);
    }

    private static SettingsReadFailureKind? ReadVersion(JsonElement root, out int version)
    {
        version = 0;
        JsonElement versionElement = default;
        int count = 0;
        bool conflictingDuplicate = false;
        foreach (JsonProperty property in root.EnumerateObject().Where(
            property => property.NameEquals(SchemaVersionName)))
        {
            count++;
            if (count == 1)
            {
                versionElement = property.Value;
            }
            else if (!VersionsMatch(versionElement, property.Value))
            {
                conflictingDuplicate = true;
            }
        }
        return count == 0
            ? SettingsReadFailureKind.Incomplete
            : conflictingDuplicate
                ? SettingsReadFailureKind.UnexpectedProperty
                : versionElement.ValueKind != JsonValueKind.Number
                    ? SettingsReadFailureKind.Malformed
                    : !versionElement.TryGetInt32(out version) ||
                        version is not LegacySchemaVersion and not SupportedSchemaVersion
                        ? SettingsReadFailureKind.UnknownVersion
                        : null;
    }

    private static bool VersionsMatch(JsonElement first, JsonElement second)
    {
        return first.ValueKind == JsonValueKind.Number &&
            second.ValueKind == JsonValueKind.Number &&
            first.TryGetInt32(out int firstNumber) &&
            second.TryGetInt32(out int secondNumber) &&
            firstNumber == secondNumber;
    }

    private static SettingsReadOutcome ReadVersionOne(JsonElement root)
    {
        SettingsReadFailureKind? failure = ValidateExactProperties(
            root,
            [SchemaVersionName, HiddenItemsName, ColorSchemeName]);
        return failure is null
            ? ReadPreferences(root, BookmarkCatalog.Empty)
            : SettingsReadOutcome.Rejected(failure);
    }

    private static SettingsReadOutcome ReadVersionTwo(JsonElement root)
    {
        SettingsReadFailureKind? shapeFailure = ValidateExactProperties(
            root,
            [SchemaVersionName, HiddenItemsName, ColorSchemeName, CategoriesName, BookmarksName]);
        if (shapeFailure is not null)
        {
            return SettingsReadOutcome.Rejected(shapeFailure);
        }

        JsonElement categoriesElement = root.GetProperty(CategoriesName);
        JsonElement bookmarksElement = root.GetProperty(BookmarksName);
        return categoriesElement.ValueKind != JsonValueKind.Array ||
            bookmarksElement.ValueKind != JsonValueKind.Array
            ? SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed)
            : ReadCatalog(root, categoriesElement, bookmarksElement);
    }

    private static SettingsReadOutcome ReadCatalog(
        JsonElement root,
        JsonElement categoriesElement,
        JsonElement bookmarksElement)
    {
        if (categoriesElement.GetArrayLength() > BookmarkCatalog.MaximumCategoryCount ||
            bookmarksElement.GetArrayLength() > BookmarkCatalog.MaximumBookmarkCount)
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.InvalidBookmarks);
        }

        List<BookmarkCategoryName> categories = [];
        SettingsReadFailureKind? failure = ReadCategories(categoriesElement, categories);
        if (failure is not null)
        {
            return SettingsReadOutcome.Rejected(failure);
        }
        List<BookmarkEntry> bookmarks = [];
        failure = ReadBookmarks(bookmarksElement, bookmarks);
        if (failure is not null)
        {
            return SettingsReadOutcome.Rejected(failure);
        }
        BookmarkCatalogCreationOutcome catalog = BookmarkCatalog.Create(categories, bookmarks);
        return catalog is BookmarkCatalogAccepted accepted
            ? ReadPreferences(root, accepted.Catalog)
            : SettingsReadOutcome.Rejected(SettingsReadFailureKind.InvalidBookmarks);
    }

    private static SettingsReadFailureKind? ReadCategories(
        JsonElement input,
        List<BookmarkCategoryName> categories)
    {
        foreach (JsonElement element in input.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                return SettingsReadFailureKind.Malformed;
            }
            BookmarkCategoryNameParseOutcome parsed = BookmarkCategoryName.Parse(element.GetString());
            if (parsed is not BookmarkCategoryNameAccepted accepted)
            {
                return SettingsReadFailureKind.InvalidBookmarks;
            }
            categories.Add(accepted.Name);
        }
        return null;
    }

    private static SettingsReadFailureKind? ReadBookmarks(
        JsonElement input,
        List<BookmarkEntry> bookmarks)
    {
        foreach (JsonElement element in input.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return SettingsReadFailureKind.Malformed;
            }
            SettingsReadFailureKind? shapeFailure = ValidateExactProperties(
                element,
                [BookmarkName, BookmarkPathName, BookmarkCategoryPropertyName, BookmarkSlotName]);
            if (shapeFailure is not null)
            {
                return shapeFailure;
            }
            SettingsReadFailureKind? entryFailure = ReadBookmark(element, out BookmarkEntry? bookmark);
            if (entryFailure is not null || bookmark is null)
            {
                return entryFailure ?? SettingsReadFailureKind.InvalidBookmarks;
            }
            bookmarks.Add(bookmark);
        }
        return null;
    }

    private static SettingsReadFailureKind? ReadBookmark(
        JsonElement element,
        out BookmarkEntry? bookmark)
    {
        bookmark = null;
        JsonElement nameElement = element.GetProperty(BookmarkName);
        JsonElement pathElement = element.GetProperty(BookmarkPathName);
        if (nameElement.ValueKind != JsonValueKind.String ||
            pathElement.ValueKind != JsonValueKind.String)
        {
            return SettingsReadFailureKind.Malformed;
        }
        BookmarkDisplayNameParseOutcome name = BookmarkDisplayName.Parse(nameElement.GetString());
        BookmarkPathParseOutcome path = BookmarkPath.Parse(pathElement.GetString());
        if (name is not BookmarkDisplayNameAccepted acceptedName ||
            path is not BookmarkPathAccepted acceptedPath)
        {
            return SettingsReadFailureKind.InvalidBookmarks;
        }
        SettingsReadFailureKind? categoryFailure = ReadCategory(
            element.GetProperty(BookmarkCategoryPropertyName),
            out BookmarkCategoryName? category);
        SettingsReadFailureKind? slotFailure = ReadSlot(
            element.GetProperty(BookmarkSlotName),
            out BookmarkShortcutSlot? slot);
        if (categoryFailure is not null || slotFailure is not null)
        {
            return categoryFailure ?? slotFailure;
        }
        bookmark = BookmarkEntry.Create(acceptedName.Name, acceptedPath.Path, category, slot);
        return null;
    }

    private static SettingsReadFailureKind? ReadCategory(
        JsonElement element,
        out BookmarkCategoryName? category)
    {
        category = null;
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (element.ValueKind != JsonValueKind.String)
        {
            return SettingsReadFailureKind.Malformed;
        }
        BookmarkCategoryNameParseOutcome parsed = BookmarkCategoryName.Parse(element.GetString());
        if (parsed is not BookmarkCategoryNameAccepted accepted)
        {
            return SettingsReadFailureKind.InvalidBookmarks;
        }
        category = accepted.Name;
        return null;
    }

    private static SettingsReadFailureKind? ReadSlot(
        JsonElement element,
        out BookmarkShortcutSlot? slot)
    {
        slot = null;
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (element.ValueKind != JsonValueKind.Number)
        {
            return SettingsReadFailureKind.Malformed;
        }
        if (!element.TryGetInt32(out int number) ||
            BookmarkShortcutSlot.Parse(number) is not BookmarkShortcutSlotAccepted accepted)
        {
            return SettingsReadFailureKind.InvalidBookmarks;
        }
        slot = accepted.Slot;
        return null;
    }

    private static SettingsReadOutcome ReadPreferences(JsonElement root, BookmarkCatalog catalog)
    {
        JsonElement hiddenItems = root.GetProperty(HiddenItemsName);
        JsonElement colorScheme = root.GetProperty(ColorSchemeName);
        HiddenItemVisibility? visibility = hiddenItems.ValueKind == JsonValueKind.True
            ? HiddenItemVisibility.Shown
            : hiddenItems.ValueKind == JsonValueKind.False
                ? HiddenItemVisibility.Hidden
                : null;
        return visibility is null || colorScheme.ValueKind != JsonValueKind.String
            ? SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed)
            : ColorScheme.Parse(colorScheme.GetString()) is ColorSchemeAccepted accepted
                ? SettingsReadOutcome.Read(UserSettings.Create(accepted.Scheme, visibility, catalog))
                : SettingsReadOutcome.Rejected(SettingsReadFailureKind.UnknownColorScheme);
    }

    private static SettingsReadFailureKind? ValidateExactProperties(
        JsonElement element,
        IReadOnlyList<string> requiredNames)
    {
        HashSet<string> allowed = new(requiredNames, StringComparer.Ordinal);
        HashSet<string> missing = new(requiredNames, StringComparer.Ordinal);
        bool duplicate = false;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                return SettingsReadFailureKind.UnexpectedProperty;
            }
            if (!missing.Remove(property.Name))
            {
                duplicate = true;
            }
        }
        return missing.Count > 0
            ? SettingsReadFailureKind.Incomplete
            : duplicate
                ? SettingsReadFailureKind.UnexpectedProperty
                : null;
    }

    private static ReadOnlyMemory<byte> WithoutUtf8Preamble(ReadOnlyMemory<byte> input)
    {
        ReadOnlySpan<byte> bytes = input.Span;
        return bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf
            ? input[3..]
            : input;
    }

    private static bool IsJsonWhitespace(ReadOnlySpan<byte> input)
    {
        foreach (byte value in input)
        {
            if (value is not 0x20 and not 0x09 and not 0x0a and not 0x0d)
            {
                return false;
            }
        }
        return true;
    }
}
