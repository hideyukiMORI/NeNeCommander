using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Represents all render-ready state of the settings modal and its separate warning.</summary>
public sealed record SettingsPresentation
{
    private readonly SchemeOptions _schemes;

    internal SettingsPresentation(
        SettingsSnapshot source,
        IReadOnlyList<SettingsColorSchemeOption> schemes,
        SettingsSaveStatus saveStatus,
        SettingsWarningPresentation warning)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(schemes);
        ArgumentNullException.ThrowIfNull(saveStatus);
        ArgumentNullException.ThrowIfNull(warning);
        IsOpen = source.Editor == SettingsEditorState.Open;
        ShowHiddenItemsAtLaunch = source.Settings.Display.HiddenItemVisibility == HiddenItemVisibility.Shown;
        ShowKeyHints = source.Settings.Display.KeyHintVisibility == KeyHintVisibility.Shown;
        _schemes = SchemeOptions.Create(schemes);
        Languages = LanguageOptions.Create(InterfaceLanguage.All.Select(language =>
            new SettingsLanguageOption(language, source.Settings.Display.InterfaceLanguage)));
        SaveStatus = saveStatus;
        Warning = warning;
    }

    /// <summary>Gets whether the settings modal is open.</summary>
    public bool IsOpen { get; }

    /// <summary>Gets the current next-launch hidden-item checkbox value.</summary>
    public bool ShowHiddenItemsAtLaunch { get; }

    /// <summary>Gets the current normal footer hint checkbox value.</summary>
    public bool ShowKeyHints { get; }

    /// <summary>Gets all 129 scheme options in canonical order.</summary>
    public IReadOnlyList<SettingsColorSchemeOption> Schemes => _schemes;

    /// <summary>Gets the shipped language options in their canonical order.</summary>
    public IReadOnlyList<SettingsLanguageOption> Languages { get; }

    /// <summary>Gets the localized save-on-change status.</summary>
    public SettingsSaveStatus SaveStatus { get; }

    /// <summary>Gets the independent persistent warning.</summary>
    public SettingsWarningPresentation Warning { get; }

    private sealed record SchemeOptions : IReadOnlyList<SettingsColorSchemeOption>
    {
        private readonly SettingsColorSchemeOption[] _options;

        private SchemeOptions(IEnumerable<SettingsColorSchemeOption> options)
        {
            _options = [.. options];
        }

        public int Count => _options.Length;

        public SettingsColorSchemeOption this[int index] => _options[index];

        internal static SchemeOptions Create(IEnumerable<SettingsColorSchemeOption> options)
        {
            return new SchemeOptions(options);
        }

        public IEnumerator<SettingsColorSchemeOption> GetEnumerator()
        {
            return ((IEnumerable<SettingsColorSchemeOption>)_options).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed record LanguageOptions : IReadOnlyList<SettingsLanguageOption>
    {
        private readonly SettingsLanguageOption[] _options;

        private LanguageOptions(IEnumerable<SettingsLanguageOption> options)
        {
            _options = [.. options];
        }

        public int Count => _options.Length;

        public SettingsLanguageOption this[int index] => _options[index];

        internal static LanguageOptions Create(IEnumerable<SettingsLanguageOption> options)
        {
            return new LanguageOptions(options);
        }

        public IEnumerator<SettingsLanguageOption> GetEnumerator()
        {
            return ((IEnumerable<SettingsLanguageOption>)_options).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
