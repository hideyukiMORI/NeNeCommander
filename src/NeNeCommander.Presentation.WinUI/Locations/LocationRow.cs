using System;
using NeNeCommander.Application.Locations;

namespace NeNeCommander.Presentation.WinUI.Locations;

/// <summary>One localized, render-ready Locations picker entry with the item a selection submits.</summary>
public sealed record LocationRow
{
    internal LocationRow(
        LocationItem item,
        string nameText,
        string detailText,
        string automationName,
        string automationId)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameText);
        ArgumentNullException.ThrowIfNull(detailText);
        ArgumentNullException.ThrowIfNull(automationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        Item = item;
        NameText = nameText;
        DetailText = detailText;
        AutomationName = automationName;
        AutomationId = automationId;
    }

    /// <summary>Gets the Application entry a selection of this row submits.</summary>
    public LocationItem Item { get; }

    /// <summary>Gets the drive designator or distribution name.</summary>
    public string NameText { get; }

    /// <summary>Gets the localized label and kind of a drive, or the localized WSL marker.</summary>
    public string DetailText { get; }

    /// <summary>Gets the complete localized UIA name.</summary>
    public string AutomationName { get; }

    /// <summary>Gets the stable automation identity derived from the entry name.</summary>
    public string AutomationId { get; }
}
