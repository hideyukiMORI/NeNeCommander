using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves Application value and gateway boundaries reject absent required arguments.</summary>
[TestClass]
public sealed class NullGuardTests
{
    /// <summary>Proves synchronous public factories and reducers reject each absent required argument.</summary>
    [TestMethod]
    public void InvokeWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        FileSystemPath path = NullGuardAssertions.ParsePath("C:\\source");
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity")).Identity;
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        FileEntrySnapshot snapshot = FileEntrySnapshot.Create(path, identity, DeletionCapability.Recycle);
        DirectoryEntry entry = DirectoryEntry.Create(
            path,
            "source",
            DirectoryEntryKind.File,
            EntryMetadata.Unmeasured(EntryVisibility.Normal));
        PaneState state = Assert.IsInstanceOfType<PaneStateAccepted>(
            PaneState.Create(path, [entry], capacity, HiddenItemVisibility.Hidden)).State;
        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryListingAccepted>(
            DirectoryListing.Create(path, [entry], DirectoryListingCompleteness.Complete, 0)).Listing;

        AssertFileOperationValueArguments(path, identity);
        AssertPaneAndEntryArguments(path, entry, capacity, state);
        AssertDirectoryAndNavigationArguments(path, entry, capacity, listing);
        AssertPaneProjectionArguments(state, entry);
        AssertSettingsValueArguments();
        ConstructorInfo constructor = typeof(FileOperationGateway).GetConstructor([typeof(IFileOperationPort)]) ??
            throw new AssertFailedException("The public gateway constructor was not found.");
        TargetInvocationException constructorFailure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructor.Invoke([null]));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(constructorFailure.InnerException);

        Assert.AreSame(snapshot.Path, path);
        Assert.AreSame(listing.Entries[0], entry);
    }

    /// <summary>Proves the asynchronous gateway rejects an absent request before provider access.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenRequestIsNullThrowsArgumentNullException()
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);
        MethodInfo method = NullGuardAssertions.GetSinglePublicStaticOrInstanceMethod(
            typeof(FileOperationGateway),
            nameof(FileOperationGateway.ExecuteAsync));

        object? invocation = method.Invoke(gateway, [null, RecordingFileOperationProgress.Create(), CancellationToken.None]);
        Task task = Assert.IsInstanceOfType<Task>(invocation);
        FileOperationRequestCreation creation = DeleteRequest.Create([NullGuardAssertions.ParsePath("C:\\source")], null);
        FileOperationRequest request = Assert.IsInstanceOfType<FileOperationRequestAccepted>(creation).Request;
        object? withoutObserver = method.Invoke(gateway, [request, null, CancellationToken.None]);
        Task withoutObserverTask = Assert.IsInstanceOfType<Task>(withoutObserver);

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await task);
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await withoutObserverTask);
        Assert.IsEmpty(port.Calls);
    }

    private static void AssertFileOperationValueArguments(FileSystemPath path, FileIdentity identity)
    {
        NullGuardAssertions.AssertStaticNullGuard(typeof(FileEntrySnapshot), nameof(FileEntrySnapshot.Create),
            [null, identity, DeletionCapability.Recycle]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(FileEntrySnapshot), nameof(FileEntrySnapshot.Create),
            [path, null, DeletionCapability.Recycle]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(FileEntrySnapshot), nameof(FileEntrySnapshot.Create),
            [path, identity, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(FileInspectionOutcome), nameof(FileInspectionOutcome.Succeeded), [null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(FileInspectionOutcome), nameof(FileInspectionOutcome.Failed), [null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(MoveRequest), nameof(MoveRequest.Create), [null, path]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(MoveRequest), nameof(MoveRequest.Create), [new[] { path }, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(CopyRequest), nameof(CopyRequest.Create), [null, path]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(CopyRequest), nameof(CopyRequest.Create), [new[] { path }, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(CreateDirectoryRequest), nameof(CreateDirectoryRequest.Create), [null, "name"]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(CreateDirectoryRequest), nameof(CreateDirectoryRequest.Create), [path, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(RenameRequest), nameof(RenameRequest.Create), [null, "name"]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(RenameRequest), nameof(RenameRequest.Create), [path, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(UserIntent), nameof(UserIntent.SubmitName), [null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(UserIntent), nameof(UserIntent.BeginAddressEdit), [null]);
        NullGuardAssertions.AssertStaticNullGuard(
            typeof(UserIntent),
            nameof(UserIntent.SubmitAddress),
            [null, "C:\\target"]);
        NullGuardAssertions.AssertStaticNullGuard(
            typeof(UserIntent),
            nameof(UserIntent.SubmitAddress),
            [AddressEditorState.Closed, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(UserIntent), nameof(UserIntent.CancelAddress), [null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(DeleteRequest), nameof(DeleteRequest.Create), [null, null]);
        NullGuardAssertions.AssertStaticNullGuard(
            typeof(PermanentDeletionConfirmation),
            nameof(PermanentDeletionConfirmation.CreateFor),
            [null]);
        AssertFileOperationFailureArguments();

    }

    private static void AssertFileOperationFailureArguments()
    {
        NullGuardAssertions.AssertStaticNullGuard(typeof(ProviderStepOutcome), nameof(ProviderStepOutcome.Failed), [null]);
        NullGuardAssertions.AssertStaticNullGuard(
            typeof(AtomicMoveCapabilityOutcome),
            nameof(AtomicMoveCapabilityOutcome.Failed),
            [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(AtomicMoveCapabilityFailed), [null]);
        NullGuardAssertions.AssertStaticNullGuard(
            typeof(ProviderStepOutcome),
            nameof(ProviderStepOutcome.FailedAfterEffect),
            [null, ProviderStepEffectKind.CopyTargetCreated]);
        NullGuardAssertions.AssertStaticNullGuard(
            typeof(ProviderStepOutcome),
            nameof(ProviderStepOutcome.FailedAfterEffect),
            [FileOperationFailureKind.Copy, null]);
    }

    private static void AssertPaneAndEntryArguments(FileSystemPath path, DirectoryEntry entry, VisiblePageCapacity capacity, PaneState state)
    {
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneState), nameof(PaneState.Create),
            [null, new[] { entry }, capacity, HiddenItemVisibility.Hidden]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneState), nameof(PaneState.Create),
            [path, null, capacity, HiddenItemVisibility.Hidden]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneState), nameof(PaneState.Create),
            [path, new[] { entry }, null, HiddenItemVisibility.Hidden]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneState), nameof(PaneState.Create),
            [path, new[] { entry }, capacity, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Apply), [null, UserIntent.MoveNext]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Apply), [state, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(DirectoryEntry), nameof(DirectoryEntry.Create),
            [null, "name", DirectoryEntryKind.File, entry.Metadata]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(DirectoryEntry), nameof(DirectoryEntry.Create),
            [path, null, DirectoryEntryKind.File, entry.Metadata]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(DirectoryEntry), nameof(DirectoryEntry.Create),
            [path, "name", null, entry.Metadata]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(DirectoryEntry), nameof(DirectoryEntry.Create),
            [path, "name", DirectoryEntryKind.File, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(EntryMetadata), nameof(EntryMetadata.Create),
            [null, EntrySize.Unknown, EntryTimestamp.Unknown]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(EntryMetadata), nameof(EntryMetadata.Create),
            [EntryVisibility.Normal, null, EntryTimestamp.Unknown]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(EntryMetadata), nameof(EntryMetadata.Create),
            [EntryVisibility.Normal, EntrySize.Unknown, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(EntryMetadata), nameof(EntryMetadata.Unmeasured), [null]);
    }

    private static void AssertDirectoryAndNavigationArguments(FileSystemPath path, DirectoryEntry entry, VisiblePageCapacity capacity, DirectoryListing listing)
    {
        NullGuardAssertions.AssertStaticNullGuard(typeof(DirectoryListing), nameof(DirectoryListing.Create),
            [null, new[] { entry }, DirectoryListingCompleteness.Complete, 0]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(DirectoryListing), nameof(DirectoryListing.Create),
            [path, null, DirectoryListingCompleteness.Complete, 0]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(DirectoryListing), nameof(DirectoryListing.Create),
            [path, new[] { entry }, null, 0]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(DirectoryReadRequest), nameof(DirectoryReadRequest.Create), [null, 1]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(DirectoryReadOutcome), nameof(DirectoryReadOutcome.Succeeded), [null, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(DirectoryReadOutcome), nameof(DirectoryReadOutcome.Failed), [null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(FileLaunchOutcome), nameof(FileLaunchOutcome.Failed), [null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Navigate),
            [null, capacity, null, HiddenItemVisibility.Hidden]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Navigate),
            [listing, null, null, HiddenItemVisibility.Hidden]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Navigate),
            [listing, capacity, null, null]);
    }

    private static void AssertPaneProjectionArguments(PaneState state, DirectoryEntry entry)
    {
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.ApplyHiddenItemVisibility),
            [null, HiddenItemVisibility.Hidden]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.ApplyHiddenItemVisibility),
            [state, null]);
        NullGuardAssertions.AssertInternalMethodNullGuard(typeof(PaneReducer), nameof(PaneReducer.ApplySortOrder), null,
            [null, PaneSortOrder.Default, null]);
        NullGuardAssertions.AssertInternalMethodNullGuard(typeof(PaneReducer), nameof(PaneReducer.ApplySortOrder), null,
            [state, null, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(EntryOrdering), nameof(EntryOrdering.Apply),
            [null, PaneSortOrder.Default]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(EntryOrdering), nameof(EntryOrdering.Apply),
            [new[] { entry }, null]);
        NullGuardAssertions.AssertInstanceNullGuard(PaneSortOrder.Default, nameof(PaneSortOrder.Toggle), [null]);
    }

    private static void AssertSettingsValueArguments()
    {
        NullGuardAssertions.AssertStaticNullGuard(typeof(UserSettings), nameof(UserSettings.Create),
            [null, BookmarkCatalog.Empty, PaneSortPreferences.Default]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(UserSettings), nameof(UserSettings.Create),
            [DisplayPreferences.Default, null, PaneSortPreferences.Default]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(UserSettings), nameof(UserSettings.Create),
            [DisplayPreferences.Default, BookmarkCatalog.Empty, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneSortPreferences), nameof(PaneSortPreferences.Create),
            [null, PaneSortOrder.Default]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneSortPreferences), nameof(PaneSortPreferences.Create),
            [PaneSortOrder.Default, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneSortOrder), nameof(PaneSortOrder.Create),
            [null, SortDirection.Ascending]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneSortOrder), nameof(PaneSortOrder.Create),
            [SortKey.Name, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(SettingsReadOutcome), nameof(SettingsReadOutcome.Read), [null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(SettingsReadOutcome), nameof(SettingsReadOutcome.Rejected), [null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(SettingsWriteOutcome), nameof(SettingsWriteOutcome.Rejected),
            [null, SettingsDirectoryEffect.NotAttempted, SettingsWriteEffect.None]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(SettingsWriteOutcome), nameof(SettingsWriteOutcome.Rejected),
            [SettingsWriteFailureKind.IoFailure, null, SettingsWriteEffect.None]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(SettingsWriteOutcome), nameof(SettingsWriteOutcome.Rejected),
            [SettingsWriteFailureKind.IoFailure, SettingsDirectoryEffect.NotAttempted, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(SettingsPersistenceState), nameof(SettingsPersistenceState.StartupRejected),
            [null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(SettingsPersistenceState), nameof(SettingsPersistenceState.Failed), [null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(UserIntent), nameof(UserIntent.SelectColorScheme), [null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(UserIntent), nameof(UserIntent.SelectLaunchHiddenItemVisibility), [null]);

    }
}
