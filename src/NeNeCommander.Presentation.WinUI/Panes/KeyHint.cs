namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents one displayed shortcut hint: the localization resource that labels the key cap and
/// the one that names what the key does. Both come from resources, so no hint text is assembled in
/// code (CS-025) and no view holds a private binding (KBD-005).
/// </summary>
public sealed record KeyHint
{
    internal KeyHint(string keyLabelResourceKey, string intentLabelResourceKey, string automationId)
    {
        KeyLabelResourceKey = keyLabelResourceKey;
        IntentLabelResourceKey = intentLabelResourceKey;
        AutomationId = automationId;
    }

    /// <summary>Gets the localization resource key of the key-cap label.</summary>
    public string KeyLabelResourceKey { get; }

    /// <summary>Gets the localization resource key that names the intent.</summary>
    public string IntentLabelResourceKey { get; }

    /// <summary>Gets the stable identifier of a declared hint target, or empty when it declares none.</summary>
    public string AutomationId { get; }
}
