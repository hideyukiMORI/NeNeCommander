using System;
using System.Linq;
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
        SettingsReadFailureKind? languageFailure = ReadInterfaceLanguage(root, out InterfaceLanguage? language);
        if (languageFailure is not null || language is null)
        {
            return languageFailure ?? SettingsReadFailureKind.Malformed;
        }
        display = DisplayPreferences.Create(accepted.Scheme, visibility, hints, language);
        return null;
    }

    private static SettingsReadFailureKind? ReadInterfaceLanguage(
        JsonElement root,
        out InterfaceLanguage? language)
    {
        language = InterfaceLanguage.System;
        if (!root.TryGetProperty("uiLanguage", out JsonElement choice))
        {
            return null;
        }
        language = null;
        if (choice.ValueKind != JsonValueKind.String)
        {
            return SettingsReadFailureKind.Malformed;
        }
        string? identifier = choice.GetString();
        language = InterfaceLanguage.All.FirstOrDefault(candidate =>
            string.Equals(candidate.Identifier, identifier, StringComparison.Ordinal));
        return language is null ? SettingsReadFailureKind.UnknownInterfaceLanguage : null;
    }

    private static KeyHintVisibility? ReadKeyHintVisibility(JsonElement root)
    {
        return !root.TryGetProperty("showKeyHints", out JsonElement choice) || choice.ValueKind == JsonValueKind.True
            ? KeyHintVisibility.Shown
            : choice.ValueKind == JsonValueKind.False ? KeyHintVisibility.Hidden : null;
    }
}
