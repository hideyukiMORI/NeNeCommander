using System;
using Microsoft.Windows.ApplicationModel.Resources;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.App.Views;

/// <summary>Owns one window-scoped context over the shipped PRI; changing its language never writes settings.</summary>
public sealed partial class ViewTextResources
{
    private readonly ResourceManager _manager;
    private readonly ResourceMap _strings;
    private readonly ResourceContext _context;
    private readonly string _systemLanguage;
    private InterfaceLanguage? _selected;

    /// <summary>Captures the untouched system qualifier before the host applies its closed settings choice.</summary>
    public ViewTextResources()
    {
        _manager = new ResourceManager();
        _strings = _manager.MainResourceMap.GetSubtree("Resources");
        _context = _manager.CreateResourceContext();
        _systemLanguage = _context.QualifierValues["Language"];
    }

    internal event EventHandler? Changed;

    internal string LanguageTag => _strings.GetValue("CommanderWindowTitle", _context).QualifierValues["Language"];

    internal string GetString(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _strings.GetValue(key.Replace('.', '/'), _context).ValueAsString;
    }

    internal void ApplyLanguage(InterfaceLanguage language)
    {
        ArgumentNullException.ThrowIfNull(language);
        if (_selected == language)
        {
            return;
        }
        _context.QualifierValues["Language"] = language == InterfaceLanguage.System ? _systemLanguage : language.Identifier;
        _selected = language;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
