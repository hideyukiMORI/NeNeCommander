using System.Text.Json;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>Translates validated settings-root properties into complete display choices.</summary>
internal static class SettingsDisplayCodec
{
    internal static SettingsReadFailureKind? Read(JsonElement root, out DisplayPreferences? display)
    {
        display = null;
        JsonElement hiddenItems = root.GetProperty("showHiddenItems");
        JsonElement colorScheme = root.GetProperty("colorScheme");
        HiddenItemVisibility? visibility = hiddenItems.ValueKind == JsonValueKind.True
            ? HiddenItemVisibility.Shown
            : hiddenItems.ValueKind == JsonValueKind.False ? HiddenItemVisibility.Hidden : null;
        KeyHintVisibility? hints = ReadKeyHintVisibility(root);
        if (visibility is null || hints is null || colorScheme.ValueKind != JsonValueKind.String)
        {
            return SettingsReadFailureKind.Malformed;
        }
        if (ColorScheme.Parse(colorScheme.GetString()) is not ColorSchemeAccepted accepted)
        {
            return SettingsReadFailureKind.UnknownColorScheme;
        }
        display = DisplayPreferences.Create(accepted.Scheme, visibility, hints);
        return null;
    }

    private static KeyHintVisibility? ReadKeyHintVisibility(JsonElement root)
    {
        return !root.TryGetProperty("showKeyHints", out JsonElement choice) || choice.ValueKind == JsonValueKind.True
            ? KeyHintVisibility.Shown
            : choice.ValueKind == JsonValueKind.False ? KeyHintVisibility.Hidden : null;
    }
}
