using System.Linq;
using System.Text.Json;
using NeNeCommander.Application.Settings;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>Translates the exact nullable schema-six placement object at the settings boundary.</summary>
internal static class SettingsWindowPlacementCodec
{
    internal static SettingsReadFailureKind? Read(JsonElement root, out SavedWindowPlacement? placement)
    {
        placement = null;
        if (!root.TryGetProperty("windowPlacement", out JsonElement element) || element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (element.ValueKind != JsonValueKind.Object)
        {
            return SettingsReadFailureKind.Malformed;
        }
        SettingsReadFailureKind? shape = SettingsDocumentValidator.ValidateExactProperties(
            element, ["left", "top", "width", "height", "state"]);
        return shape ?? ReadPlacement(element, out placement);
    }

    internal static void Write(Utf8JsonWriter writer, SavedWindowPlacement? placement)
    {
        if (placement is null)
        {
            writer.WriteNull("windowPlacement");
            return;
        }
        writer.WriteStartObject("windowPlacement");
        writer.WriteNumber("left", placement.Bounds.Left);
        writer.WriteNumber("top", placement.Bounds.Top);
        writer.WriteNumber("width", placement.Bounds.Width);
        writer.WriteNumber("height", placement.Bounds.Height);
        writer.WriteString("state", placement.State == WindowPresenterState.Maximized ? "maximized" : "restored");
        writer.WriteEndObject();
    }

    private static SettingsReadFailureKind? ReadPlacement(JsonElement element, out SavedWindowPlacement? placement)
    {
        placement = null;
        if (element.EnumerateObject().Any(property => property.Name != "state" && property.Value.ValueKind != JsonValueKind.Number) ||
            element.GetProperty("state").ValueKind != JsonValueKind.String)
        {
            return SettingsReadFailureKind.Malformed;
        }
        if (!element.GetProperty("left").TryGetInt32(out int left) ||
            !element.GetProperty("top").TryGetInt32(out int top) ||
            !element.GetProperty("width").TryGetInt32(out int width) ||
            !element.GetProperty("height").TryGetInt32(out int height) || width < 1 || height < 1)
        {
            return SettingsReadFailureKind.Malformed;
        }
        WindowPresenterState? state = element.GetProperty("state").GetString() switch
        {
            "restored" => WindowPresenterState.Restored,
            "maximized" => WindowPresenterState.Maximized,
            _ => null,
        };
        if (state is null)
        {
            return SettingsReadFailureKind.Malformed;
        }
        placement = SavedWindowPlacement.Create(WindowBounds.Create(left, top, width, height), state);
        return null;
    }
}
