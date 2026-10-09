using System;
using System.Collections.Generic;
using System.Globalization;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Locations;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Wsl;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Locations;

/// <summary>
/// Projects the Application-owned Locations picker state into localized rows and texts, and
/// qualifies a mapped picker key with the exact state the host rendered. It owns no resources, no
/// listing, and no navigation decision.
/// </summary>
public static class LocationsPresenter
{
    private static readonly Dictionary<DriveKind, string> KindLabels = new()
    {
        [DriveKind.Fixed] = "LocationsDriveKindFixed",
        [DriveKind.Removable] = "LocationsDriveKindRemovable",
        [DriveKind.Network] = "LocationsDriveKindNetwork",
        [DriveKind.Optical] = "LocationsDriveKindOptical",
        [DriveKind.Unknown] = "LocationsDriveKindUnknown",
    };

    private static readonly Dictionary<DriveCatalogFailureKind, string> DriveFailureLabels = new()
    {
        [DriveCatalogFailureKind.ProviderUnavailable] = "LocationsDrivesProviderUnavailable",
        [DriveCatalogFailureKind.AccessDenied] = "LocationsDrivesAccessDenied",
    };

    private static readonly Dictionary<WslDistributionCatalogFailureKind, string> WslFailureLabels = new()
    {
        [WslDistributionCatalogFailureKind.ProviderUnavailable] = "LocationsWslProviderUnavailable",
        [WslDistributionCatalogFailureKind.MalformedOutput] = "LocationsWslMalformedOutput",
    };

    /// <summary>Creates the complete presentation of one Application-owned picker state.</summary>
    /// <param name="state">Exact state the host is rendering.</param>
    /// <param name="localize">Resource lookup owned by the host.</param>
    /// <returns>The complete immutable presentation.</returns>
    public static LocationsPresentation Present(LocationsState state, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(localize);
        IReadOnlyList<KeyHint> hints = KeyHintPresenter.Present(KeyboardContext.Locations);
        if (state is not LocationsOpen open)
        {
            string status = state is LocationsLoading ? localize("LocationsLoading") : string.Empty;
            return new LocationsPresentation(
                state,
                new LocationsPresentationTexts(status, string.Empty, string.Empty),
                [],
                hints);
        }
        List<LocationRow> rows = [];
        foreach (LocationItem item in open.Items)
        {
            rows.Add(Row(item, localize));
        }
        LocationsPresentationTexts texts = new(
            open.Items.Count == 0 ? localize("LocationsEmpty") : string.Empty,
            DrivesText(open.Drives, localize),
            open.WslRoots is WslRootSectionFailed wslFailed
                ? localize(Declared(WslFailureLabels, wslFailed.Failure))
                : string.Empty);
        return new LocationsPresentation(open, texts, rows.AsReadOnly(), hints);
    }

    /// <summary>
    /// Qualifies one intent the canonical mapper produced in the picker context with the exact open
    /// state the host rendered: <c>Enter</c> selects the focused entry and <c>Escape</c> closes the
    /// picker. Every other intent, and every intent while the picker is not open, is returned
    /// unchanged, so the session decides it.
    /// </summary>
    /// <param name="intent">Intent the canonical mapper produced.</param>
    /// <param name="rendered">State the host last rendered.</param>
    /// <returns>The intent to forward to the session.</returns>
    public static UserIntent Qualify(UserIntent intent, LocationsState rendered)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(rendered);
        return rendered is not LocationsOpen open
            ? intent
            : intent == UserIntent.Escape
                ? UserIntent.CancelLocations(open)
                : intent == UserIntent.Confirm && open.FocusItem is LocationItem focus
                    ? UserIntent.SelectLocation(open, focus)
                    : intent;
    }

    private static LocationRow Row(LocationItem item, Func<string, string> localize)
    {
        string name;
        string detail;
        if (item is DriveLocationItem drive)
        {
            name = drive.Drive.Root.Drive;
            string kind = localize(Declared(KindLabels, drive.Drive.Kind));
            detail = drive.Drive.VolumeLabel is string label
                ? Format(localize("LocationsDriveDetailFormat"), label, kind)
                : kind;
        }
        else
        {
            name = ((WslLocationItem)item).Root.DistributionName;
            detail = localize("LocationsWslDetail");
        }
        return new LocationRow(
            item,
            name,
            detail,
            Format(localize("LocationsRowAutomationNameFormat"), name, detail),
            "LocationsRow_" + name);
    }

    private static string DrivesText(DriveSection drives, Func<string, string> localize)
    {
        return drives is DriveSectionFailed failed
            ? localize(Declared(DriveFailureLabels, failed.Failure))
            : drives is DriveSectionListed { UnrepresentableRootCount: > 0 and int count }
                ? Format(localize("LocationsDrivesUnrepresentableFormat"), count)
                : string.Empty;
    }

    private static string Declared<TKey>(Dictionary<TKey, string> labels, TKey key)
        where TKey : notnull
    {
        return labels.TryGetValue(key, out string? label)
            ? label
            : throw new InvalidOperationException("The Locations state value is not supported.");
    }

    private static string Format(string format, params object[] values)
    {
        return string.Format(CultureInfo.InvariantCulture, format, values);
    }
}
