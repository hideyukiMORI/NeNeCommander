using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Owns isolated pane sessions and their deterministic ports for the related tests.</summary>
internal sealed class DualPaneSessionTestContext : IDisposable
{
    private DualPaneSessionTestContext(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        ScriptedFileOperationPort port,
        FileOperationGateway gateway)
    {
        Left = left;
        Right = right;
        Port = port;
        Gateway = gateway;
        LeftSession = new PaneSession(left,
        new ScriptedFileLauncher(),
        PaneSessionConfiguration.Create(Capacity(4), DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        RightSession = new PaneSession(right,
        new ScriptedFileLauncher(),
        PaneSessionConfiguration.Create(Capacity(4), DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        Panes = new DualPaneSession(LeftSession, RightSession, gateway);
    }

    internal ScriptedDirectoryReadPort Left { get; }

    internal ScriptedDirectoryReadPort Right { get; }

    internal ScriptedFileOperationPort Port { get; }

    internal FileOperationGateway Gateway { get; }

    internal PaneSession LeftSession { get; }

    internal PaneSession RightSession { get; }

    internal DualPaneSession Panes { get; }

    internal static DualPaneSessionTestContext Create()
    {
        return Create(null, null);
    }

    internal static DualPaneSessionTestContext Create(ScriptedCallbackPoint? callbackPoint, Action? callback)
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(callbackPoint, callback);
        return new DualPaneSessionTestContext(
            ScriptedDirectoryReadPort.Create(),
            ScriptedDirectoryReadPort.Create(),
            port,
            new FileOperationGateway(port));
    }

    internal async Task ListBothAsync(DirectoryListing leftListing, DirectoryListing rightListing)
    {
        Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        Right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        _ = await Panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await Panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
    }

    public void Dispose()
    {
        Gateway.Dispose();
    }

    internal static FileInspectionOutcome Inspection(FileSystemPath path)
    {
        return Inspection(path, DeletionCapability.Recycle);
    }

    internal static FileInspectionOutcome Inspection(FileSystemPath path, DeletionCapability capability)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(path, identity.Identity, capability));
    }

    internal static FileSystemPath? Focus(PaneSnapshot snapshot)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content).State.FocusItem;
    }

    internal static VisiblePageCapacity Capacity(int rows)
    {
        return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(rows)).Capacity;
    }

    internal static DirectoryListing Listing(string location, params (string Name, DirectoryEntryKind Kind)[] entries)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] built = new DirectoryEntry[entries.Length];
        for (int index = 0; index < entries.Length; index++)
        {
            string separator = parsedLocation.CanonicalText.EndsWith('\\') ? string.Empty : "\\";
            built[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + separator + entries[index].Name),
                entries[index].Name,
                entries[index].Kind,
                EntryMetadata.Unmeasured(EntryVisibility.Normal));
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            built,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    internal static DirectoryListing ListingWithVisibility(
        string location,
        params (string Name, DirectoryEntryKind Kind, EntryVisibility Visibility)[] entries)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] built = new DirectoryEntry[entries.Length];
        for (int index = 0; index < entries.Length; index++)
        {
            string separator = parsedLocation.CanonicalText.EndsWith('\\') ? string.Empty : "\\";
            built[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + separator + entries[index].Name),
                entries[index].Name,
                entries[index].Kind,
                EntryMetadata.Unmeasured(entries[index].Visibility));
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            built,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    internal static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
