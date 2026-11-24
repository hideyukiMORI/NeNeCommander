using System;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Represents one shipped next-launch language radio option.</summary>
public sealed record SettingsLanguageOption
{
    internal SettingsLanguageOption(InterfaceLanguage language, InterfaceLanguage selectedLanguage)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(selectedLanguage);
        Language = language;
        IsSelected = language == selectedLanguage;
        NameResourceKey = language.Identifier switch
        {
            "system" => "SettingsLanguageSystem",
            "ja-JP" => "SettingsLanguageJapanese",
            "en-US" => "SettingsLanguageEnglish",
            "zh-Hans" => "SettingsLanguageSimplifiedChinese",
            _ => throw new InvalidOperationException("The settings editor received an unshipped language."),
        };
        AutomationId = "SettingsLanguage" + language.Identifier;
    }

    /// <summary>Gets the closed language emitted by selection.</summary>
    public InterfaceLanguage Language { get; }

    /// <summary>Gets the localized or native language name key.</summary>
    public string NameResourceKey { get; }

    /// <summary>Gets the stable identifier of this option independent of its translated name.</summary>
    public string AutomationId { get; }

    /// <summary>Gets whether this is the selected next-launch value.</summary>
    public bool IsSelected { get; }
}
