using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Shares typed local snapshots and reflection guard assertions across operation fixtures.</summary>
internal static class WindowsLocalOperationTestContext
{
    internal static void AssertNullGuard(object instance, string methodName, object?[] arguments)
    {
        MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance) ??
            throw new AssertFailedException("The port method was not found.");
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(instance, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    internal static void AssertResolverNullGuard(MethodInfo method, object?[] arguments, string parameterName)
    {
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, arguments));
        ArgumentNullException inner = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
        Assert.AreEqual(parameterName, inner.ParamName);
    }

    internal static async Task<FileEntrySnapshot> InspectAsync(WindowsLocalFileOperationAdapter adapter, FileSystemPath path)
    {
        FileInspectionOutcome outcome = await adapter.InspectAsync(path, CancellationToken.None);
        return Assert.IsInstanceOfType<FileInspectionSucceeded>(outcome).Snapshot;
    }

    internal static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
