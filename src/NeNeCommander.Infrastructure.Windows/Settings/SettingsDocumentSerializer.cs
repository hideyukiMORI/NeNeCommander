using System.IO;
using System.Text.Json;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>Serializes the complete current settings schema in one fixed property order.</summary>
internal static class SettingsDocumentSerializer
{
    internal static byte[] Serialize(UserSettings settings)
    {
        using MemoryStream output = new();
        using (Utf8JsonWriter writer = new(output))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 3);
            writer.WriteBoolean(
                "showHiddenItems",
                settings.HiddenItemVisibility == HiddenItemVisibility.Shown);
            writer.WriteString("colorScheme", settings.ColorScheme.Identifier);
            writer.WriteStartArray("bookmarkCategories");
            foreach (BookmarkCategoryName category in settings.Bookmarks.Categories)
            {
                writer.WriteStringValue(category.Value);
            }
            writer.WriteEndArray();
            writer.WriteStartArray("bookmarks");
            foreach (BookmarkEntry bookmark in settings.Bookmarks.Bookmarks)
            {
                WriteBookmark(writer, bookmark);
            }
            writer.WriteEndArray();
            SettingsSortOrderCodec.Write(writer, settings.PaneSortOrders);
            writer.WriteEndObject();
        }
        return output.ToArray();
    }

    private static void WriteBookmark(Utf8JsonWriter writer, BookmarkEntry bookmark)
    {
        writer.WriteStartObject();
        writer.WriteString("name", bookmark.Name.Value);
        writer.WriteString("path", bookmark.Path.Value.CanonicalText);
        if (bookmark.Category is null)
        {
            writer.WriteNull("category");
        }
        else
        {
            writer.WriteString("category", bookmark.Category.Value);
        }
        if (bookmark.ShortcutSlot is null)
        {
            writer.WriteNull("shortcutSlot");
        }
        else
        {
            writer.WriteNumber("shortcutSlot", bookmark.ShortcutSlot.Number);
        }
        writer.WriteEndObject();
    }

}
