using System;
using System.Text.Json;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>Owns the strict schema-three representation of the two typed pane sort orders.</summary>
internal static class SettingsSortOrderCodec
{
    internal static SettingsReadFailureKind? Read(JsonElement element, out PaneSortPreferences? preferences)
    {
        preferences = null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return SettingsReadFailureKind.Malformed;
        }
        SettingsReadFailureKind? shape = SettingsDocumentValidator.ValidateExactProperties(element, ["left", "right"]);
        if (shape is not null)
        {
            return shape;
        }
        SettingsReadFailureKind? leftFailure = ReadOrder(element.GetProperty("left"), out PaneSortOrder? left);
        SettingsReadFailureKind? rightFailure = ReadOrder(element.GetProperty("right"), out PaneSortOrder? right);
        if (leftFailure is not null || rightFailure is not null || left is null || right is null)
        {
            return leftFailure ?? rightFailure ?? SettingsReadFailureKind.Malformed;
        }
        preferences = PaneSortPreferences.Create(left, right);
        return null;
    }

    internal static void Write(Utf8JsonWriter writer, PaneSortPreferences preferences)
    {
        writer.WriteStartObject("paneSortOrders");
        WriteOrder(writer, "left", preferences.Left);
        WriteOrder(writer, "right", preferences.Right);
        writer.WriteEndObject();
    }

    private static SettingsReadFailureKind? ReadOrder(JsonElement element, out PaneSortOrder? order)
    {
        order = null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return SettingsReadFailureKind.Malformed;
        }
        SettingsReadFailureKind? shape = SettingsDocumentValidator.ValidateExactProperties(element, ["key", "direction"]);
        if (shape is not null)
        {
            return shape;
        }
        JsonElement keyElement = element.GetProperty("key");
        JsonElement directionElement = element.GetProperty("direction");
        if (keyElement.ValueKind != JsonValueKind.String || directionElement.ValueKind != JsonValueKind.String)
        {
            return SettingsReadFailureKind.Malformed;
        }
        SortKey? key = keyElement.GetString() switch
        {
            "name" => SortKey.Name,
            "extension" => SortKey.Extension,
            "size" => SortKey.Size,
            "modified" => SortKey.Modified,
            _ => null,
        };
        SortDirection? direction = directionElement.GetString() switch
        {
            "ascending" => SortDirection.Ascending,
            "descending" => SortDirection.Descending,
            _ => null,
        };
        if (key is null || direction is null)
        {
            return SettingsReadFailureKind.Malformed;
        }
        order = PaneSortOrder.Create(key, direction);
        return null;
    }

    private static void WriteOrder(Utf8JsonWriter writer, string name, PaneSortOrder order)
    {
        writer.WriteStartObject(name);
        writer.WriteString("key", KeyText(order.Key));
        writer.WriteString("direction", DirectionText(order.Direction));
        writer.WriteEndObject();
    }

    private static string KeyText(SortKey key)
    {
        return key == SortKey.Name ? "name"
            : key == SortKey.Extension ? "extension"
            : key == SortKey.Size ? "size"
            : key == SortKey.Modified ? "modified"
            : throw new InvalidOperationException("The sort key is not a declared choice.");
    }

    private static string DirectionText(SortDirection direction)
    {
        return direction == SortDirection.Ascending ? "ascending"
            : direction == SortDirection.Descending ? "descending"
            : throw new InvalidOperationException("The sort direction is not a declared choice.");
    }
}
