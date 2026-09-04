using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Diagnostics;
using NeNeCommander.Infrastructure.Windows.FileOperations;
using NeNeCommander.Infrastructure.Windows.Paths;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves every Infrastructure boundary rejects absent required values.</summary>
[TestClass]
public sealed class NullGuardTests
{
    /// <summary>Proves public functions reject each absent required argument.</summary>
    [TestMethod]
    public void InvokeWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        FileSystemPath path = ParsePath("C:\\source");
        DiagnosticSalt salt = Assert.IsInstanceOfType<DiagnosticSaltAccepted>(
            DiagnosticSalt.Parse("0123456789abcdef0123456789abcdef")).Salt;

        AssertStaticNullGuard(typeof(DiagnosticPathFingerprint), nameof(DiagnosticPathFingerprint.Create),
            [null, salt]);
        AssertStaticNullGuard(typeof(DiagnosticPathFingerprint), nameof(DiagnosticPathFingerprint.Create),
            [path, null]);
        AssertStaticNullGuard(typeof(ProviderPathContainment), nameof(ProviderPathContainment.Evaluate),
            [null, path]);
        AssertStaticNullGuard(typeof(ProviderPathContainment), nameof(ProviderPathContainment.Evaluate),
            [path, null]);
        WindowsLocalPath local = Assert.IsInstanceOfType<WindowsLocalPath>(path);
        FileSystemInfo entry = new DirectoryInfo(local.CanonicalText);
        AssertStaticNullGuard(typeof(WindowsLocalEntryIdentity), nameof(WindowsLocalEntryIdentity.Find), [null]);
        AssertStaticNullGuard(typeof(WindowsLocalEntryIdentity), nameof(WindowsLocalEntryIdentity.Describe), [null]);
        AssertStaticNullGuard(typeof(WindowsLocalEntryIdentity), nameof(WindowsLocalEntryIdentity.Revalidate), [null]);
        AssertStaticNullGuard(typeof(WindowsLocalTreeCopy), nameof(WindowsLocalTreeCopy.IsReparsePoint), [null]);
        AssertStaticNullGuard(typeof(WindowsLocalTreeCopy), nameof(WindowsLocalTreeCopy.ContainsReparsePoint), [null]);
        AssertStaticNullGuard(typeof(WindowsLocalTreeCopy), nameof(WindowsLocalTreeCopy.Copy), [null, "target"]);
        AssertStaticNullGuard(typeof(WindowsLocalTreeCopy), nameof(WindowsLocalTreeCopy.Copy), [entry, null]);
        AssertStaticNullGuard(typeof(WindowsLocalTreeCopy), nameof(WindowsLocalTreeCopy.Matches), [null, "target"]);
        AssertStaticNullGuard(typeof(WindowsLocalTreeCopy), nameof(WindowsLocalTreeCopy.Matches), [entry, null]);
    }

    /// <summary>Proves closed result constructors preserve their internal null invariants.</summary>
    [TestMethod]
    public void ConstructWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        AssertConstructorNullGuard(typeof(ContainedPath));
        AssertConstructorNullGuard(typeof(RejectedPathContainment));
        AssertConstructorNullGuard(typeof(EntryMatched));
        AssertConstructorNullGuard(typeof(EntryRejected));
    }

    /// <summary>Proves the settings store rejects an absent composed document location.</summary>
    [TestMethod]
    public void ConstructWhenSettingsLocationIsNullThrowsArgumentNullException()
    {
        ConstructorInfo constructor = typeof(WindowsLocalSettingsStore).GetConstructor([typeof(WindowsLocalPath)]) ??
            throw new AssertFailedException("The public settings store constructor was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructor.Invoke([null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static void AssertStaticNullGuard(Type type, string methodName, object?[] arguments)
    {
        MethodInfo[] methods = [.. type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name.Equals(methodName, StringComparison.Ordinal))];
        Assert.HasCount(1, methods);
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => methods[0].Invoke(null, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static void AssertConstructorNullGuard(Type type)
    {
        ConstructorInfo[] constructors = [.. type
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(constructor => constructor.GetParameters() is [{ ParameterType: Type parameterType }] &&
                parameterType != type)];
        Assert.HasCount(1, constructors);
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructors[0].Invoke([null]));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
