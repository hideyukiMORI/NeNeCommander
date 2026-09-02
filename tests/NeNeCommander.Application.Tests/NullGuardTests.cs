using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        ConstructorInfo constructor = typeof(FileOperationGateway).GetConstructor([typeof(IFileOperationPort)]) ??
            throw new AssertFailedException("The public gateway constructor was not found.");
        TargetInvocationException constructorFailure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructor.Invoke([null]));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(constructorFailure.InnerException);

        Assert.AreSame(snapshot.Path, path);
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
