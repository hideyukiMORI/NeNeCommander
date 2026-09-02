using System.Text.Json;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>Owns strict, bounded validation of the current persisted settings schema.</summary>
public static class SettingsDocumentValidator
{
    private const int MaximumDocumentLength = 65536;

    /// <summary>Validates untrusted settings text without applying partial state.</summary>
    /// <param name="input">Untrusted persisted JSON text.</param>
    /// <returns>A complete accepted marker or a typed rejection.</returns>
    public static SettingsValidationOutcome Validate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new SettingsValidationRejected(SettingsValidationFailureKind.Empty);
        }
        if (input.Length > MaximumDocumentLength)
        {
            return new SettingsValidationRejected(SettingsValidationFailureKind.TooLarge);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(input);
            return ValidateRoot(document.RootElement);
        }
        catch (JsonException)
        {
            return new SettingsValidationRejected(SettingsValidationFailureKind.Malformed);
        }
    }

    private static SettingsValidationOutcome ValidateRoot(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return new SettingsValidationRejected(SettingsValidationFailureKind.Malformed);
        }

        int schemaVersionCount = 0;
        int hiddenVisibilityCount = 0;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.NameEquals("schemaVersion"))
            {
                schemaVersionCount++;
                if (!property.Value.TryGetInt32(out int version) || version != 1)
                {
                    return new SettingsValidationRejected(SettingsValidationFailureKind.UnknownVersion);
                }
            }
            else if (property.NameEquals("showHiddenItems"))
            {
                hiddenVisibilityCount++;
                if (property.Value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
                {
                    return new SettingsValidationRejected(SettingsValidationFailureKind.Malformed);
                }
            }
            else
            {
                return new SettingsValidationRejected(SettingsValidationFailureKind.UnexpectedProperty);
            }
        }

        return schemaVersionCount > 1 || hiddenVisibilityCount > 1
            ? new SettingsValidationRejected(SettingsValidationFailureKind.UnexpectedProperty)
            : schemaVersionCount == 1 && hiddenVisibilityCount == 1
                ? new SettingsValidationAccepted()
                : new SettingsValidationRejected(SettingsValidationFailureKind.Incomplete);
    }
}
