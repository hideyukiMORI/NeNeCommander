using System.Text.Json;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// Owns strict, bounded validation of the current persisted settings schema and is the sole
/// place where untrusted settings text becomes typed <see cref="UserSettings"/>.
/// </summary>
public static class SettingsDocumentValidator
{
    /// <summary>Maximum accepted document length in characters; longer text is rejected unparsed.</summary>
    public const int MaximumDocumentLength = 65536;

    private const int SupportedSchemaVersion = 1;
    private const int RequiredPropertyCount = 3;
    private const string SchemaVersionName = "schemaVersion";
    private const string HiddenItemsName = "showHiddenItems";
    private const string ColorSchemeName = "colorScheme";

    /// <summary>Validates untrusted settings text without applying partial state.</summary>
    /// <param name="input">Untrusted persisted JSON text.</param>
    /// <returns>Complete settings or a typed rejection.</returns>
    public static SettingsReadOutcome Validate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Empty);
        }
        if (input.Length > MaximumDocumentLength)
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.TooLarge);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(input);
            return ValidateRoot(document.RootElement);
        }
        catch (JsonException)
        {
            return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed);
        }
    }

    private static SettingsReadOutcome ValidateRoot(JsonElement root)
    {
        return root.ValueKind == JsonValueKind.Object
            ? ValidatePropertyNames(root)
            : SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed);
    }

    /// <summary>
    /// Rejects any name outside the schema, then uses the property count to separate a duplicated
    /// property from a missing one before any value is interpreted.
    /// </summary>
    private static SettingsReadOutcome ValidatePropertyNames(JsonElement root)
    {
        int propertyCount = 0;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (!IsSchemaName(property))
            {
                return SettingsReadOutcome.Rejected(SettingsReadFailureKind.UnexpectedProperty);
            }
            propertyCount++;
        }

        return propertyCount > RequiredPropertyCount
            ? SettingsReadOutcome.Rejected(SettingsReadFailureKind.UnexpectedProperty)
            : propertyCount < RequiredPropertyCount
                ? SettingsReadOutcome.Rejected(SettingsReadFailureKind.Incomplete)
                : ReadDocument(root);
    }

    private static bool IsSchemaName(JsonProperty property)
    {
        return property.NameEquals(SchemaVersionName) ||
            property.NameEquals(HiddenItemsName) ||
            property.NameEquals(ColorSchemeName);
    }

    private static SettingsReadOutcome ReadDocument(JsonElement root)
    {
        return !root.TryGetProperty(SchemaVersionName, out JsonElement version) ||
            !root.TryGetProperty(HiddenItemsName, out JsonElement hiddenItems) ||
            !root.TryGetProperty(ColorSchemeName, out JsonElement colorScheme)
            ? SettingsReadOutcome.Rejected(SettingsReadFailureKind.Incomplete)
            : ReadVersionedDocument(version, hiddenItems, colorScheme);
    }

    private static SettingsReadOutcome ReadVersionedDocument(
        JsonElement version,
        JsonElement hiddenItems,
        JsonElement colorScheme)
    {
        return version.ValueKind != JsonValueKind.Number
            ? SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed)
            : !version.TryGetInt32(out int schemaVersion) || schemaVersion != SupportedSchemaVersion
                ? SettingsReadOutcome.Rejected(SettingsReadFailureKind.UnknownVersion)
                : ReadHiddenItemVisibility(hiddenItems, colorScheme);
    }

    private static SettingsReadOutcome ReadHiddenItemVisibility(JsonElement hiddenItems, JsonElement colorScheme)
    {
        return hiddenItems.ValueKind == JsonValueKind.True
            ? ReadColorScheme(colorScheme, HiddenItemVisibility.Shown)
            : hiddenItems.ValueKind == JsonValueKind.False
                ? ReadColorScheme(colorScheme, HiddenItemVisibility.Hidden)
                : SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed);
    }

    private static SettingsReadOutcome ReadColorScheme(JsonElement colorScheme, HiddenItemVisibility visibility)
    {
        return colorScheme.ValueKind != JsonValueKind.String
            ? SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed)
            : ColorScheme.Parse(colorScheme.GetString()) is ColorSchemeAccepted accepted
                ? SettingsReadOutcome.Read(UserSettings.Create(accepted.Scheme, visibility))
                : SettingsReadOutcome.Rejected(SettingsReadFailureKind.UnknownColorScheme);
    }
}
