using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves every public Application boundary rejects absent required collaborators and values.</summary>
[TestClass]
public sealed class NullGuardTests
{
    /// <summary>Proves synchronous public factories and reducers reject each absent required argument.</summary>
    [TestMethod]
    public void InvokeWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        FileSystemPath path = ParsePath("C:\\source");
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity")).Identity;
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        FileEntrySnapshot snapshot = FileEntrySnapshot.Create(path, identity, DeletionCapability.Recycle);
        PaneState state = Assert.IsInstanceOfType<PaneStateAccepted>(
            PaneState.Create(path, [path], capacity)).State;
        DirectoryEntry entry = DirectoryEntry.Create(path, "source", DirectoryEntryKind.File);
        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryListingAccepted>(
            DirectoryListing.Create(path, [entry], DirectoryListingCompleteness.Complete, 0)).Listing;

        AssertStaticNullGuard(typeof(FileEntrySnapshot), nameof(FileEntrySnapshot.Create),
            [null, identity, DeletionCapability.Recycle]);
        AssertStaticNullGuard(typeof(FileEntrySnapshot), nameof(FileEntrySnapshot.Create),
            [path, null, DeletionCapability.Recycle]);
        AssertStaticNullGuard(typeof(FileEntrySnapshot), nameof(FileEntrySnapshot.Create),
            [path, identity, null]);
        AssertStaticNullGuard(typeof(FileInspectionOutcome), nameof(FileInspectionOutcome.Succeeded), [null]);
        AssertStaticNullGuard(typeof(FileInspectionOutcome), nameof(FileInspectionOutcome.Failed), [null]);
        AssertStaticNullGuard(typeof(MoveRequest), nameof(MoveRequest.Create), [null, path]);
        AssertStaticNullGuard(typeof(MoveRequest), nameof(MoveRequest.Create), [new[] { path }, null]);
        AssertStaticNullGuard(typeof(CopyRequest), nameof(CopyRequest.Create), [null, path]);
        AssertStaticNullGuard(typeof(CopyRequest), nameof(CopyRequest.Create), [new[] { path }, null]);
        AssertStaticNullGuard(typeof(DeleteRequest), nameof(DeleteRequest.Create), [null, null]);
        AssertStaticNullGuard(
            typeof(PermanentDeletionConfirmation),
            nameof(PermanentDeletionConfirmation.CreateFor),
            [null]);
        AssertStaticNullGuard(typeof(ProviderStepOutcome), nameof(ProviderStepOutcome.Failed), [null]);
        AssertStaticNullGuard(typeof(PaneState), nameof(PaneState.Create), [null, new[] { path }, capacity]);
        AssertStaticNullGuard(typeof(PaneState), nameof(PaneState.Create), [path, null, capacity]);
        AssertStaticNullGuard(typeof(PaneState), nameof(PaneState.Create), [path, new[] { path }, null]);
        AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Apply), [null, UserIntent.MoveNext]);
        AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Apply), [state, null]);
        AssertStaticNullGuard(typeof(DirectoryEntry), nameof(DirectoryEntry.Create), [null, "name", DirectoryEntryKind.File]);
        AssertStaticNullGuard(typeof(DirectoryEntry), nameof(DirectoryEntry.Create), [path, null, DirectoryEntryKind.File]);
        AssertStaticNullGuard(typeof(DirectoryEntry), nameof(DirectoryEntry.Create), [path, "name", null]);
        AssertStaticNullGuard(typeof(DirectoryListing), nameof(DirectoryListing.Create),
            [null, new[] { entry }, DirectoryListingCompleteness.Complete, 0]);
        AssertStaticNullGuard(typeof(DirectoryListing), nameof(DirectoryListing.Create),
            [path, null, DirectoryListingCompleteness.Complete, 0]);
        AssertStaticNullGuard(typeof(DirectoryListing), nameof(DirectoryListing.Create),
            [path, new[] { entry }, null, 0]);
        AssertStaticNullGuard(typeof(DirectoryReadRequest), nameof(DirectoryReadRequest.Create), [null, 1]);
        AssertStaticNullGuard(typeof(DirectoryReadOutcome), nameof(DirectoryReadOutcome.Succeeded), [null]);
        AssertStaticNullGuard(typeof(DirectoryReadOutcome), nameof(DirectoryReadOutcome.Failed), [null]);
        AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Navigate), [null, capacity, null]);
        AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Navigate), [listing, null, null]);

        ConstructorInfo constructor = typeof(FileOperationGateway).GetConstructor([typeof(IFileOperationPort)]) ??
            throw new AssertFailedException("The public gateway constructor was not found.");
        TargetInvocationException constructorFailure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructor.Invoke([null]));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(constructorFailure.InnerException);

        Assert.AreSame(snapshot.Path, path);
        Assert.AreSame(listing.Entries[0], entry);
    }

    /// <summary>Proves the pane session rejects absent collaborators and arguments before any read.</summary>
    [TestMethod]
    public void PaneSessionWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        ConstructorInfo constructor = typeof(PaneSession).GetConstructor(
            [typeof(IDirectoryReadPort), typeof(VisiblePageCapacity), typeof(int)]) ??
            throw new AssertFailedException("The public session constructor was not found.");
        PaneSession session = new(port, capacity, DirectoryListing.EntryBoundaryLimit);

        AssertConstructorNullGuard(constructor, [null, capacity, 1]);
        AssertConstructorNullGuard(constructor, [port, null, 1]);
        AssertInstanceNullGuard(session, nameof(PaneSession.NavigateAsync), [null, CancellationToken.None]);
        AssertInstanceNullGuard(session, nameof(PaneSession.HandleAsync), [null, CancellationToken.None]);
        Assert.IsEmpty(port.Requests);
    }

    /// <summary>Proves the dual-pane coordinator and its snapshot reject absent collaborators and arguments.</summary>
    [TestMethod]
    public async Task DualPaneSessionWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        FileOperationRequest cancelledRequest = Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            DeleteRequest.Create([ParsePath("C:\\source")], null)).Request;
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        PaneSession left = new(ScriptedDirectoryReadPort.Create(), capacity, DirectoryListing.EntryBoundaryLimit);
        PaneSession right = new(ScriptedDirectoryReadPort.Create(), capacity, DirectoryListing.EntryBoundaryLimit);
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        FileOperationOutcome outcome = await gateway.ExecuteAsync(cancelledRequest, cancellation.Token);
        ConstructorInfo constructor = typeof(DualPaneSession).GetConstructor(
            [typeof(PaneSession), typeof(PaneSession), typeof(FileOperationGateway)]) ??
            throw new AssertFailedException("The public dual-pane constructor was not found.");
        DualPaneSession panes = new(left, right, gateway);
        FileSystemPath path = ParsePath("C:\\source");

        AssertConstructorNullGuard(constructor, [null, right, gateway]);
        AssertConstructorNullGuard(constructor, [left, null, gateway]);
        AssertConstructorNullGuard(constructor, [left, right, null]);
        AssertInstanceNullGuard(panes, nameof(DualPaneSession.NavigateAsync), [null, path, CancellationToken.None]);
        AssertInstanceNullGuard(panes, nameof(DualPaneSession.HandleAsync), [null, CancellationToken.None]);
        AssertInstanceNullGuard(panes.Current, nameof(DualPaneSnapshot.Of), [null]);
        AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [null, PaneSnapshot.Initial, PaneSide.Left, OperationActivity.Idle]);
        AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [PaneSnapshot.Initial, null, PaneSide.Left, OperationActivity.Idle]);
        AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [PaneSnapshot.Initial, PaneSnapshot.Initial, null, OperationActivity.Idle]);
        AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [PaneSnapshot.Initial, PaneSnapshot.Initial, PaneSide.Left, null]);
        AssertInternalConstructorNullGuard(typeof(OperationRunning), [null]);
        AssertInternalConstructorNullGuard(typeof(OperationAwaitingConfirmation), [null]);
        AssertInternalConstructorNullGuard(typeof(OperationCompleted), [null, outcome]);
        AssertInternalConstructorNullGuard(typeof(OperationCompleted), [OperationKind.Move, null]);
        AssertInternalConstructorNullGuard(typeof(OperationRequestRejected), [null, FileOperationRequestFailureKind.EmptySources]);
        AssertInternalConstructorNullGuard(typeof(OperationRequestRejected), [OperationKind.Move, null]);
    }
    /// <summary>Proves internal pane state records preserve their null invariants for every collaborator.</summary>
    [TestMethod]
    public void ConstructPaneStateRecordsWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        FileSystemPath path = ParsePath("C:\\source");
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryListingAccepted>(
            DirectoryListing.Create(path, [], DirectoryListingCompleteness.Complete, 0)).Listing;
        PaneState state = PaneReducer.Navigate(listing, capacity, null);

        AssertInternalConstructorNullGuard(typeof(PaneContentListed), [null, listing]);
        AssertInternalConstructorNullGuard(typeof(PaneContentListed), [state, null]);
        AssertInternalConstructorNullGuard(typeof(PaneLoading), [null]);
        AssertInternalConstructorNullGuard(typeof(PaneReadCancelled), [null]);
        AssertInternalConstructorNullGuard(typeof(PaneReadFailed), [null, FileOperationFailureKind.NotFound]);
        AssertInternalConstructorNullGuard(typeof(PaneReadFailed), [path, null]);
        AssertInternalMethodNullGuard(typeof(PaneSnapshot), nameof(PaneSnapshot.IdleWith), null, [null]);
        AssertInternalMethodNullGuard(typeof(PaneSnapshot), nameof(PaneSnapshot.WithActivity), PaneSnapshot.Initial, [null]);
    }

    private static void AssertInternalConstructorNullGuard(Type type, object?[] arguments)
    {
        ConstructorInfo[] constructors = [.. type
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(constructor => constructor.GetParameters().Length == arguments.Length &&
                constructor.GetParameters().All(parameter => parameter.ParameterType != type))];
        Assert.HasCount(1, constructors);
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructors[0].Invoke(arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static void AssertInternalMethodNullGuard(Type type, string methodName, object? instance, object?[] arguments)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) ??
            throw new AssertFailedException("The internal method was not found.");
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(instance, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }
    private static void AssertConstructorNullGuard(ConstructorInfo constructor, object?[] arguments)
    {
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructor.Invoke(arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static void AssertInstanceNullGuard(object instance, string methodName, object?[] arguments)
    {
        MethodInfo method = GetSinglePublicStaticOrInstanceMethod(instance.GetType(), methodName);
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(instance, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }
    /// <summary>Proves the asynchronous gateway rejects an absent request before provider access.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenRequestIsNullThrowsArgumentNullException()
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);
        MethodInfo method = GetSinglePublicStaticOrInstanceMethod(
            typeof(FileOperationGateway),
            nameof(FileOperationGateway.ExecuteAsync));

        object? invocation = method.Invoke(gateway, [null, CancellationToken.None]);
        Task task = Assert.IsInstanceOfType<Task>(invocation);

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await task);
        Assert.IsEmpty(port.Calls);
    }

    private static void AssertStaticNullGuard(Type type, string methodName, object?[] arguments)
    {
        MethodInfo method = GetSinglePublicStaticOrInstanceMethod(type, methodName);
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static MethodInfo GetSinglePublicStaticOrInstanceMethod(Type type, string methodName)
    {
        MethodInfo[] methods = [.. type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.Name.Equals(methodName, StringComparison.Ordinal))];
        Assert.HasCount(1, methods);
        return methods[0];
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
