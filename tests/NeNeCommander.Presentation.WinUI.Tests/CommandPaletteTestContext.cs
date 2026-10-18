using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Supplies deterministic captured palette scopes and synthetic localization for palette tests.</summary>
internal static class CommandPaletteTestContext
{
    internal static async Task<CommandPaletteOpen> OpenPaletteAsync(
        DirectoryReadOutcome? passiveListing)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt"), null));
        if (passiveListing is not null)
        {
            right.Enqueue(passiveListing);
        }
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(4)).Capacity;
        PaneSession leftPane = new(left,
            new AcceptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        PaneSession rightPane = new(right,
            new AcceptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        using FileOperationGateway gateway = new(QueuedFileOperationPort.Create());
        CommanderSession session = new(
            new DualPaneSession(leftPane, rightPane, gateway),
            new SettingsSession(
                new SuccessfulSettingsStore(),
                SettingsReadOutcome.Absent(),
                static _ => { }),
            new TransientScopeOwners(
                new AddressEditorSession(),
                new CommandPaletteSession(),
                new WindowAdjustmentSession(),
                new LocationsSession(new PendingDriveCatalog(), new PendingWslDistributionCatalog())));
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        if (passiveListing is not null)
        {
            _ = await session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        }
        CommanderSnapshot snapshot = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            new SilentCommanderObserver(),
            CancellationToken.None);
        return Assert.IsInstanceOfType<CommandPaletteOpen>(snapshot.Scopes.CommandPalette);
    }

    internal static DirectoryListing Listing(string location, string name)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry entry = DirectoryEntry.Create(
            ParsePath(location + "\\" + name),
            name,
            DirectoryEntryKind.File,
            EntryMetadata.Unmeasured(EntryVisibility.Normal));
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(
            parsedLocation,
            [entry],
            DirectoryListingCompleteness.Complete,
            0)).Listing;
    }

    private static FileSystemPath ParsePath(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    internal static string Localize(string key)
    {
        Dictionary<string, string> values = new()
        {
            ["IntentLabelOpenFocused"] = "Open item",
            ["IntentLabelCopy"] = "Copy Files",
            ["KeyLabelL"] = "L",
            ["KeyLabelF5"] = "F5",
            ["CommandPaletteTargetLeft"] = "Target: left pane",
            ["CommandPaletteTargetRight"] = "Target: right pane",
            ["CommandPaletteOppositeLeft"] = "Opposite: left pane",
            ["CommandPaletteOppositeRight"] = "Opposite: right pane",
            ["CommandPaletteUnavailableDetailFormat"] = "{0}; {1}; Unavailable: {2}",
            ["CommandPaletteUnavailableAutomationNameFormat"] =
                "{0}; {1}; {2}; {3}; Unavailable; {4}",
            ["CommandPaletteUnavailableMarker"] = "!",
            ["CommandUnavailablePassivePane"] = "Opposite pane is unavailable",
        };
        return values.TryGetValue(key, out string? value) ? value : key;
    }

    internal static string ProjectResource(string key)
    {
        return key switch
        {
            "CommandPaletteUnavailableDetailFormat" => "{0}|{1}|{2}",
            "CommandPaletteUnavailableAutomationNameFormat" => "{0};{1};{2};{3};{4}",
            _ => $"<{key}>",
        };
    }

    internal static CommandUnavailable CreateUnavailable(CommandUnavailableReason reason)
    {
        return InvokeInternal<CommandUnavailable>(reason);
    }

    internal static CommandCandidate CreateCandidate(
        UserIntent intent,
        CommandAvailability availability)
    {
        return InvokeInternal<CommandCandidate>(intent, availability);
    }

    internal static CommandPaletteOpen CreateOpen(
        CommandPaletteOpen template,
        PaneSide activeSide,
        IReadOnlyList<CommandCandidate> candidates)
    {
        return InvokeInternal<CommandPaletteOpen>(
            template.Left,
            template.Right,
            activeSide,
            candidates);
    }

    private static T InvokeInternal<T>(params object[] arguments)
    {
        ConstructorInfo constructor = typeof(T).GetConstructors(
            BindingFlags.Instance | BindingFlags.NonPublic).Single(candidate =>
                candidate.GetParameters().Length == arguments.Length &&
                (arguments.Length != 1 || candidate.GetParameters()[0].ParameterType != typeof(T)));
        return (T)constructor.Invoke(arguments);
    }

    private sealed class SuccessfulSettingsStore : ISettingsStore
    {
        public Task<SettingsReadOutcome> ReadAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(SettingsReadOutcome.Absent());
        }

        public Task<SettingsWriteOutcome> WriteAsync(
            UserSettings settings,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(SettingsWriteOutcome.Succeeded());
        }
    }

    private sealed class SilentCommanderObserver : ICommanderProgressObserver
    {
        public void OperationProgressed(DualPaneSnapshot snapshot)
        {
        }

        public void SettingsProgressed(SettingsSnapshot snapshot)
        {
        }
    }
}
