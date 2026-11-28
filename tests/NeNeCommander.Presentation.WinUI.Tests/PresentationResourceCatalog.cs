using System;
using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Reads only the three shipped catalogs embedded as fixed test inputs, without a filesystem or ambient locale.</summary>
internal sealed class PresentationResourceCatalog
{
    private readonly ImmutableDictionary<string, string> _values;

    internal PresentationResourceCatalog(string locale)
    {
        string resource = locale switch
        {
            "en-US" => "PresentationResources.en-US.resw",
            "ja-JP" => "PresentationResources.ja-JP.resw",
            "zh-Hans" => "PresentationResources.zh-Hans.resw",
            _ => throw new ArgumentOutOfRangeException(nameof(locale)),
        };
        using XmlReader reader = XmlReader.Create(
            typeof(PresentationResourceCatalog).Assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException("The shipped catalog was not embedded."),
            new XmlReaderSettings { CloseInput = true });
        XElement root = XDocument.Load(reader).Root ?? throw new InvalidOperationException("The shipped catalog has no root.");
        _values = root.Elements("data").ToImmutableDictionary(
            element => (string?)element.Attribute("name") ?? throw new InvalidOperationException("A resource key is missing."),
            element => (string?)element.Element("value") ?? throw new InvalidOperationException("A resource value is missing."),
            StringComparer.Ordinal);
    }

    internal static PresentationResourceCatalog English { get; } = new("en-US");

    internal string Get(string key)
    {
        return _values[key];
    }
}
